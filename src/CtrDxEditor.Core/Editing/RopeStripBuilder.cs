using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>An RGBA color with straight (non-premultiplied) 0-1 channels. Channels may exceed 1 (clamped at raster time).</summary>
    /// <param name="R">The red channel, nominally 0-1; values above 1 are allowed and clamped when rasterized.</param>
    /// <param name="G">The green channel, nominally 0-1; values above 1 are allowed and clamped when rasterized.</param>
    /// <param name="B">The blue channel, nominally 0-1; values above 1 are allowed and clamped when rasterized.</param>
    /// <param name="A">The alpha channel, 0-1, straight rather than premultiplied into the color channels.</param>
    public readonly record struct RopeRgba(double R, double G, double B, double A);

    /// <summary>One triangle strip of a rendered rope: parallel position/color arrays in strip order.</summary>
    /// <param name="Points">Vertex positions in level units, in strip order.</param>
    /// <param name="Colors">Per-vertex colors, index-aligned with <paramref name="Points"/> and the same length.</param>
    public sealed record RopeStrip(Vec2[] Points, RopeRgba[] Colors);

    /// <summary>
    /// A built rope visual: the triangle strips plus the bezier sample polyline
    /// (the game's <c>drawPts</c>, which also anchors the Christmas lights).
    /// </summary>
    /// <param name="Strips">The cord's triangle strips; empty for a chain, which is drawn as sprites.</param>
    /// <param name="SamplePoints">The bezier sample polyline the cord is drawn along.</param>
    public sealed record RopeVisual(IReadOnlyList<RopeStrip> Strips, IReadOnlyList<Vec2> SamplePoints)
    {
        /// <summary>
        /// The chain links to draw instead of <see cref="Strips"/>, or empty for an ordinary rope. The
        /// game picks one of the two draw paths per bungee (<c>DrawChain</c> or <c>DrawBungee</c>), so
        /// exactly one of the two is ever populated.
        /// </summary>
        public IReadOnlyList<ChainSprite> ChainSprites { get; init; } = [];
    }

    /// <summary>
    /// Builds the triangle strips that draw a rope exactly like the game's
    /// <c>Bungee.DrawBungee</c> / <c>DrawAntialiasedLineContinued</c> (default skin,
    /// alpha 1, not highlighted). Coordinates are level-space; the rope width scales
    /// with the view like sprite art.
    /// </summary>
    public static class RopeStripBuilder
    {
        // The game loads levels at mapScale = 3 (GameScene.Show), so its rope constants (half-width 5,
        // 1-unit edge fade) live in XML x 3 world space. The editor draws in raw XML space, so divide
        // them back down. Both of these are literals in DrawBungee rather than physics constants, so
        // they hold under either model; the ones that do not live in RopePhysics.
        private const double MapScale = 3;
        private const double HalfWidth = 5 / MapScale;
        private const double EdgeFade = 1 / MapScale;

        /// <summary>
        /// Evaluates the game's <c>DrawHelper.CalcPathBezier</c>: a de Casteljau reduction
        /// where every input point is a control point (the curve only interpolates the ends).
        /// </summary>
        public static Vec2 CalcPathBezier(IReadOnlyList<Vec2> controls, double t)
        {
            int n = controls.Count;
            if (n == 0)
            {
                return default;
            }

            if (n == 1)
            {
                return controls[0];
            }

            Vec2[] scratch = new Vec2[n];
            for (int i = 0; i < n; i++)
            {
                scratch[i] = controls[i];
            }

            for (int level = n - 1; level >= 1; level--)
            {
                for (int i = 0; i < level; i++)
                {
                    scratch[i] = new Vec2(
                        (scratch[i].X * (1 - t)) + (scratch[i + 1].X * t),
                        (scratch[i].Y * (1 - t)) + (scratch[i + 1].Y * t));
                }
            }

            return scratch[0];
        }

        /// <summary>
        /// The rope's bezier control points: the physics chain the game would build for this rest length,
        /// hanging on the catenary when slack and running straight when taut. Feed these to
        /// <see cref="CalcPathBezier"/> to evaluate the cord that is actually drawn.
        /// </summary>
        /// <param name="a">First endpoint (the grab), in level units.</param>
        /// <param name="b">Second endpoint (the target), in level units.</param>
        /// <param name="length">Rope rest length, in level units.</param>
        /// <param name="physics">The level's physics model; omitting it assumes the desktop model.</param>
        /// <returns>The control points, ordered from <paramref name="a"/> to <paramref name="b"/>.</returns>
        public static Vec2[] ControlPoints(Vec2 a, Vec2 b, double length, RopePhysics? physics = null)
        {
            double restLength = (physics ?? RopePhysics.Desktop).RestLength;
            Vec2 chord = b - a;
            double distance = Math.Sqrt((chord.X * chord.X) + (chord.Y * chord.Y));

            // Bungee.RollplacingWithOffset starts from the anchor and tail, then inserts one part per
            // rest length *or part thereof* - the remainder is rolled up to a whole part before the loop
            // ends - so the chain holds 2 + ceil(len / restLen) parts, and just the two when there is no
            // length to subdivide. Rounding this down instead cost a part at almost every authored
            // length, which a chain shows directly as too few links.
            int count = length > 0 ? 2 + (int)Math.Ceiling(length / restLength) : 2;
            Vec2[] pts = new Vec2[count];
            if (length > distance)
            {
                IReadOnlyList<Vec2> sag = RopeCurve.Sample(a, b, length, count - 1);
                for (int i = 0; i < count; i++)
                {
                    pts[i] = sag[i];
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    double f = (double)i / (count - 1);
                    pts[i] = new Vec2(a.X + (chord.X * f), a.Y + (chord.Y * f));
                }
            }

            return pts;
        }

        /// <summary>
        /// Builds the triangle strips for a rope from <paramref name="a"/> (the grab) to
        /// <paramref name="b"/> (the target) with rest length <paramref name="length"/>.
        /// Slack ropes hang on the catenary; taut ropes run straight, and ropes pulled
        /// past their rest length pick up the game's red stretch tint.
        /// </summary>
        public static RopeVisual Build(Vec2 a, Vec2 b, double length, int skin = 0, RopePhysics? physics = null)
        {
            RopePhysics p = physics ?? RopePhysics.Desktop;
            Vec2 chord = b - a;
            double distance = Math.Sqrt((chord.X * chord.X) + (chord.Y * chord.Y));
            return BuildStrips(
                ControlPoints(a, b, length, p),
                RopePalette.GetDrawColors(skin, distance, length, p),
                p.SamplesPerSegment);
        }

        /// <summary>
        /// Positions of the rope's Christmas lights, ported from the game's
        /// <c>Bungee.DrawChristmasLights</c>: every 6th bezier sample point, skipping
        /// 4 points at each end of the rope.
        /// </summary>
        public static List<Vec2> ChristmasLightPoints(IReadOnlyList<Vec2> samplePoints)
        {
            List<Vec2> lights = [];
            for (int i = 4; i < samplePoints.Count - 4; i += 6)
            {
                lights.Add(samplePoints[i]);
            }
            return lights;
        }

        // Port of the DrawBungee sampling loop: bezier samples batched 4 points at a time,
        // alternating the two color tracks per batch while both ramp shade -> base.
        private static RopeVisual BuildStrips(Vec2[] pts, RopeDrawColors palette, int samplesPerSegment)
        {
            List<RopeStrip> strips = [];
            List<Vec2> samples = [];
            int sampleCount = (pts.Length - 1) * samplesPerSegment;
            double sampleStep = 1.0 / sampleCount;

            double redStep = (palette.Base1.R - palette.Shade1.R) / (sampleCount - 1);
            double greenStep = (palette.Base1.G - palette.Shade1.G) / (sampleCount - 1);
            double blueStep = (palette.Base1.B - palette.Shade1.B) / (sampleCount - 1);
            double redStepAlt = (palette.Base2.R - palette.Shade2.R) / (sampleCount - 1);
            double greenStepAlt = (palette.Base2.G - palette.Shade2.G) / (sampleCount - 1);
            double blueStepAlt = (palette.Base2.B - palette.Shade2.B) / (sampleCount - 1);
            RopeRgb track1 = palette.Shade1;
            RopeRgb track2 = palette.Shade2;
            bool useTrack1 = false; // game: flag=false -> first batch draws rgbaColor6 (track 2)

            List<Vec2> batch = new(4);
            double lx = -1, ly = 0, rx = 0, ry = 0; // continued edge vertices; lx == -1 means none yet
            double t = 0;
            while (true)
            {
                if (t > 1)
                {
                    t = 1;
                }

                Vec2 sample = CalcPathBezier(pts, t);
                samples.Add(sample);
                batch.Add(sample);
                if (batch.Count >= 4 || t == 1)
                {
                    RopeRgb c = useTrack1 ? track1 : track2;
                    RopeRgba color = new(c.R, c.G, c.B, 1);
                    for (int i = 0; i < batch.Count - 1; i++)
                    {
                        if (BuildSegmentStrip(batch[i], batch[i + 1], HalfWidth, color,
                                ref lx, ref ly, ref rx, ref ry) is { } strip)
                        {
                            strips.Add(strip);
                        }
                    }

                    int steps = batch.Count - 1;
                    Vec2 carry = batch[^1];
                    batch.Clear();
                    batch.Add(carry);
                    useTrack1 = !useTrack1;
                    track1 = new RopeRgb(track1.R + (redStep * steps), track1.G + (greenStep * steps), track1.B + (blueStep * steps));
                    track2 = new RopeRgb(track2.R + (redStepAlt * steps), track2.G + (greenStepAlt * steps), track2.B + (blueStepAlt * steps));
                }

                if (t == 1)
                {
                    break;
                }

                t += sampleStep;
            }

            return new RopeVisual(strips, samples);
        }

        // Port of DrawAntialiasedLineContinued (non-highlighted): one 10-vertex strip per
        // segment. Cross-section: transparent at +/-halfWidth, +0.15 brightened color one
        // edge-fade width in, base color on the centerline. Far edge overdrawn 2% to hide
        // seams; start edges continue from the previous segment so the ribbon is seamless.
        private static RopeStrip? BuildSegmentStrip(
            Vec2 p1, Vec2 p2, double size, RopeRgba color,
            ref double lx, ref double ly, ref double rx, ref double ry)
        {
            Vec2 dir = p2 - p1;
            if (dir.X == 0 && dir.Y == 0)
            {
                return null;
            }

            Vec2 dirOver = new(dir.X * 1.02, dir.Y * 1.02);
            double len = Math.Sqrt((dir.X * dir.X) + (dir.Y * dir.Y));
            Vec2 unit = new(-dir.Y / len, dir.X / len); // game VectPerp: (-y, x), normalized
            Vec2 n = new(unit.X * size, unit.Y * size);

            Vec2 leftFar = new(p1.X + n.X + dir.X, p1.Y + n.Y + dir.Y);
            Vec2 rightFar = new(p1.X - n.X + dir.X, p1.Y - n.Y + dir.Y);
            Vec2 leftFarOver = new(p1.X + n.X + dirOver.X, p1.Y + n.Y + dirOver.Y);
            Vec2 rightFarOver = new(p1.X - n.X + dirOver.X, p1.Y - n.Y + dirOver.Y);

            Vec2 leftStart = lx == -1 ? new Vec2(p1.X + n.X, p1.Y + n.Y) : new Vec2(lx, ly);
            Vec2 rightStart = lx == -1 ? new Vec2(p1.X - n.X, p1.Y - n.Y) : new Vec2(rx, ry);
            lx = leftFar.X;
            ly = leftFar.Y;
            rx = rightFar.X;
            ry = rightFar.Y;

            Vec2 inset = new(unit.X * EdgeFade, unit.Y * EdgeFade);
            Vec2 leftStartIn = leftStart - inset;
            Vec2 leftFarIn = leftFarOver - inset;
            Vec2 rightStartIn = rightStart + inset;
            Vec2 rightFarIn = rightFarOver + inset;

            RopeRgba bright = new(color.R + 0.15, color.G + 0.15, color.B + 0.15, color.A);
            RopeRgba fade = color with { A = 0 };

            Vec2[] points =
            [
                leftStart, leftFarOver, leftStartIn, leftFarIn, p1,
                p2, rightStartIn, rightFarIn, rightStart, rightFarOver
            ];
            RopeRgba[] colors = [fade, fade, bright, bright, color, color, bright, bright, fade, fade];
            return new RopeStrip(points, colors);
        }
    }
}
