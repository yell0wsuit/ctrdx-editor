using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Where a sprite's atlas frame maps to, in level space, for an object at (x,y).</summary>
    /// <param name="Source">The atlas sub-rect to sample.</param>
    /// <param name="Dest">Where the trimmed pixels land, in level units; this is what gets drawn.</param>
    /// <param name="Hit">The untrimmed sourceSize box centered on the object, which is larger than <paramref name="Dest"/> whenever the frame was trimmed.</param>
    public readonly record struct SpriteLayout(IntRect Source, LevelBounds Dest, LevelBounds Hit);

    /// <summary>
    /// Pure placement math. The game renders HD atlas art on a world scaled up by
    /// <see cref="MapScale"/> (GameScene.Show uses mapScale = 3f), so in level space a sprite's size is
    /// atlasPixels * scale / MapScale, where scale is the per-object scale the game applies to that
    /// sprite. Objects are center-anchored on their untrimmed sourceSize.
    /// </summary>
    public static class SpritePlacement
    {
        /// <summary>Game world scale used by the original renderer when mapping atlas pixels to level space.</summary>
        public const double MapScale = 3.0;

        /// <summary>Computes atlas source and level-space destination bounds for a sprite frame.</summary>
        /// <param name="frame">The atlas frame to place.</param>
        /// <param name="x">The sprite's center X in level units; the frame is center-anchored on its untrimmed sourceSize.</param>
        /// <param name="y">The sprite's center Y in level units.</param>
        /// <param name="scale">The per-object scale the game applies to this sprite.</param>
        /// <param name="mapScale">Atlas pixels per level unit; defaults to <see cref="MapScale"/>.</param>
        /// <returns>The atlas source rect plus the level-space destination and hit bounds.</returns>
        public static SpriteLayout Compute(
            AtlasFrame frame, double x, double y, double scale = 1.0, double mapScale = MapScale)
        {
            double s = scale / mapScale;
            double w = frame.SourceSize.W * s;
            double h = frame.SourceSize.H * s;
            double left = x - (w / 2.0);
            double top = y - (h / 2.0);

            LevelBounds hit = new(left, top, w, h);
            LevelBounds dest = new(
                left + (frame.SpriteSource.X * s),
                top + (frame.SpriteSource.Y * s),
                frame.Frame.W * s,
                frame.Frame.H * s);

            return new SpriteLayout(frame.Frame, dest, hit);
        }
    }
}
