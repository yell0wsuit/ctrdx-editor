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
    /// Builds and draws grab ropes - the game-accurate cord strips plus the seasonal Christmas lights -
    /// one grab at a time, so each rope can be layered between its hook's back and front art the way the
    /// game does (<c>Grab.DrawBack</c> then <c>Grab.Draw</c>).
    /// </summary>
    internal static class RopeRenderer
    {
        /// <summary>
        /// Builds a single grab's rope visual, or null when the grab has no resolved target (nothing to
        /// hang from). The game reads the grab's length attribute for the bungee rest length; missing/0
        /// renders as a taut straight rope.
        /// </summary>
        public static RopeVisual? BuildRope(LevelObject grab, IReadOnlyList<LevelObject> objects, bool twoParts)
        {
            if (grab.Type != "grab")
            {
                return null;
            }

            RopeTarget rope = RopeResolver.Resolve(grab, objects, twoParts);
            if (rope.Target is null)
            {
                return null;
            }

            double ropeLength = double.TryParse(
                grab.GetAttr("length"), NumberStyles.Float, CultureInfo.InvariantCulture, out double len)
                ? len
                : 0;
            return RopeStripBuilder.Build(
                new Vec2(grab.X, grab.Y), new Vec2(rope.Target.X, rope.Target.Y), ropeLength);
        }

        /// <summary>
        /// Draws one grab's rope (and its seasonal Christmas lights) at the current z-position, so callers
        /// can sandwich it between the hook's back and front layers.
        /// </summary>
        /// <param name="ctx">Target drawing context.</param>
        /// <param name="v">Current level-to-screen transform.</param>
        /// <param name="sprites">Sprite cache holding the lights atlas.</param>
        /// <param name="visual">The rope built by <see cref="BuildRope"/>.</param>
        /// <param name="ropeSeed">Per-rope seed keeping the random light frames stable across redraws.</param>
        /// <param name="opBounds">Control bounds for the custom draw operation.</param>
        public static void DrawRope(
            DrawingContext ctx, ViewTransform v, SpriteCache sprites, RopeVisual visual, int ropeSeed, Rect opBounds)
        {
            if (visual.Strips.Count > 0)
            {
                ctx.Custom(new RopeDrawOperation(opBounds, v, visual.Strips));
            }
            if (SpecialEvents.IsXmas)
            {
                DrawChristmasLights(ctx, v, sprites, RopeStripBuilder.ChristmasLightPoints(visual.SamplePoints), ropeSeed);
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
            IReadOnlyList<Vec2> lightPoints,
            int seed)
        {
            if (sprites.GetChristmasLights() is not { } art)
            {
                return;
            }

            Random frameRandom = new(seed);
            foreach (Vec2 p in lightPoints)
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
