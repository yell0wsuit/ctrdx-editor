using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Draws every grab rope in a level - the game-accurate cord strips plus the
    /// seasonal Christmas lights - as one pass under the object sprites.
    /// </summary>
    internal static class RopeRenderer
    {
        // The game shows rope Christmas lights only in Dec/Jan (SpecialEvents.IsXmas).
        // Forced on while the port is verified out of season; flip to false to restore gating.
        private const bool ForceChristmasLights = true;

        /// <summary>Draws all ropes for <paramref name="doc"/> into <paramref name="ctx"/>.</summary>
        /// <param name="ctx">Target drawing context.</param>
        /// <param name="v">Current level-to-screen transform.</param>
        /// <param name="sprites">Sprite cache holding the lights atlas.</param>
        /// <param name="doc">The level being rendered.</param>
        /// <param name="opBounds">Control bounds for the custom draw operation.</param>
        public static void Draw(DrawingContext ctx, ViewTransform v, SpriteCache sprites, LevelDocument doc, Rect opBounds)
        {
            IReadOnlyList<LevelObject> objects = doc.Objects;
            List<RopeStrip> ropeStrips = [];
            List<List<Vec2>> ropeLightPoints = [];
            foreach (LevelObject obj in objects)
            {
                if (obj.Type != "grab")
                {
                    continue;
                }

                RopeTarget rope = RopeResolver.Resolve(obj, objects, doc.TwoParts);
                if (rope.Target is null)
                {
                    continue;
                }

                // The game reads the grab's length attribute for the bungee rest length;
                // missing/0 renders as a taut straight rope.
                double ropeLength = double.TryParse(
                    obj.GetAttr("length"), NumberStyles.Float, CultureInfo.InvariantCulture, out double len)
                    ? len
                    : 0;
                RopeVisual ropeVisual = RopeStripBuilder.Build(
                    new Vec2(obj.X, obj.Y), new Vec2(rope.Target.X, rope.Target.Y), ropeLength);
                ropeStrips.AddRange(ropeVisual.Strips);
                ropeLightPoints.Add(RopeStripBuilder.ChristmasLightPoints(ropeVisual.SamplePoints));
            }
            if (ropeStrips.Count > 0)
            {
                ctx.Custom(new RopeDrawOperation(opBounds, v, ropeStrips));
            }
            if (ForceChristmasLights || SpecialEvents.IsXmas)
            {
                DrawChristmasLights(ctx, v, sprites, ropeLightPoints);
            }
        }

        // Port of Bungee.DrawChristmasLights: one random light frame per anchor point,
        // centered on the frame's trimmed rect at world pixels (level units = world / mapScale).
        // Frames are seeded per rope so they stay put across redraws (the game randomizes
        // once per bungee instance).
        private static void DrawChristmasLights(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            List<List<Vec2>> ropeLightPoints)
        {
            if (sprites.GetChristmasLights() is not { } art)
            {
                return;
            }

            for (int ropeIndex = 0; ropeIndex < ropeLightPoints.Count; ropeIndex++)
            {
                Random frameRandom = new(ropeIndex);
                foreach (Vec2 p in ropeLightPoints[ropeIndex])
                {
                    AtlasFrame frame = art.Frames[frameRandom.Next(art.Frames.Count)];
                    double w = frame.Frame.W / SpritePlacement.MapScale;
                    double h = frame.Frame.H / SpritePlacement.MapScale;
                    Vec2 tl = v.LevelToScreen(new Vec2(p.X - (w / 2), p.Y - (h / 2)));
                    Vec2 br = v.LevelToScreen(new Vec2(p.X + (w / 2), p.Y + (h / 2)));
                    ctx.DrawImage(
                        art.Bitmap,
                        new Rect(frame.Frame.X, frame.Frame.Y, frame.Frame.W, frame.Frame.H),
                        new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y));
                }
            }
        }
    }
}
