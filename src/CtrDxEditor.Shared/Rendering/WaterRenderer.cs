using System.Collections.Generic;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Draws the level's water band, matching the game's <c>WaterElement</c>.
    /// <para>
    /// The game splits water across two passes (<c>GameScene.Draw</c>): <see cref="DrawBack"/> renders
    /// under the scene objects and <see cref="DrawFront"/> over them. The runtime-only bubbles, ambient
    /// lights, spotlight, and horizontal scroll are not drawn — at t=0 both scroll offsets alias to an
    /// unshifted tile, so the static render needs no offset math.
    /// </para>
    /// </summary>
    public static class WaterRenderer
    {
        private const int QuadShadowDown = 0;
        private const int QuadShadowUp = 1;
        private const int QuadBack = 2;
        private const int QuadTop = 3;

        /// <summary>The game's SCREEN_HEIGHT (1440px) expressed in level units.</summary>
        private static double LevelScreenHeight => BackgroundPlacement.ScreenHeight / SpritePlacement.MapScale;

        /// <summary>
        /// Draws the water's back layer — the bottom shadow and the back tile — beneath the scene objects.
        /// </summary>
        /// <param name="context">Drawing surface receiving the water tiles.</param>
        /// <param name="view">Transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache containing the optional water atlas.</param>
        /// <param name="band">Bottom-pinned water band in level space.</param>
        /// <param name="levelHeight">Level height in level units, used for the bottom shadow's anchor.</param>
        public static void DrawBack(
            DrawingContext context, ViewTransform view, SpriteCache sprites, LevelBounds band, int levelHeight)
        {
            if (sprites.GetWaterArt() is not { } art)
            {
                return;
            }

            // WaterElement.DrawBack anchors the bottom shadow to the lower of the band's bottom edge and
            // the screen's bottom, mixing screen space into a world position. Reproduced as-is.
            // SCREEN_OFFSET_Y is letterbox compensation that depends on the player's window aspect, so it
            // has no meaningful editor value and is treated as 0.
            double bottomY = System.Math.Max(levelHeight, LevelScreenHeight);
            IntRect shadowDown = art.Frames[QuadShadowDown].Frame;
            IntRect shadowUp = art.Frames[QuadShadowUp].Frame;

            // The game draws quad 0 with quad 1's height, not its own. This reads like a bug and is
            // exactly what WaterElement.cs:232 does; do not "correct" it.
            DrawQuad(context, view, art, shadowDown, new LevelBounds(
                band.X,
                bottomY - (shadowDown.H / SpritePlacement.MapScale),
                band.W,
                shadowUp.H / SpritePlacement.MapScale));

            IntRect back = art.Frames[QuadBack].Frame;
            DrawQuad(context, view, art, back, new LevelBounds(
                band.X, band.Y, band.W, back.H / SpritePlacement.MapScale));
        }

        /// <summary>
        /// Draws the water's front layer — the top shadow and the surface tile — over the scene objects.
        /// </summary>
        public static void DrawFront(
            DrawingContext context, ViewTransform view, SpriteCache sprites, LevelBounds band)
        {
            if (sprites.GetWaterArt() is not { } art)
            {
                return;
            }

            IntRect shadowUp = art.Frames[QuadShadowUp].Frame;
            DrawQuad(context, view, art, shadowUp, new LevelBounds(
                band.X, band.Y, band.W, shadowUp.H / SpritePlacement.MapScale));

            IntRect top = art.Frames[QuadTop].Frame;
            DrawQuad(context, view, art, top, new LevelBounds(
                band.X, band.Y, band.W, top.H / SpritePlacement.MapScale));
        }

        /// <summary>Tiles one atlas quad across a level-space region, cropping edge tiles as the game does.</summary>
        private static void DrawQuad(
            DrawingContext context, ViewTransform view, WaterArt art, IntRect quad, LevelBounds dest)
        {
            IReadOnlyList<WaterTile> tiles = WaterGeometry.Tiles(quad, dest);
            foreach (WaterTile tile in tiles)
            {
                context.DrawImage(
                    art.Bitmap,
                    new Rect(tile.Source.X, tile.Source.Y, tile.Source.W, tile.Source.H),
                    LevelSceneRenderer.LevelRectToScreen(view, tile.Dest.X, tile.Dest.Y, tile.Dest.W, tile.Dest.H));
            }
        }
    }
}
