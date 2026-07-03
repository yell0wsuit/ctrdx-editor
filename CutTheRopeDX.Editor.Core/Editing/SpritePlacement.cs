using CutTheRopeDX.Editor.Core.Atlas;
using CutTheRopeDX.Editor.Core.Geometry;

namespace CutTheRopeDX.Editor.Core.Editing
{
    /// <summary>Where a sprite's atlas frame maps to, in level space, for an object at (x,y).</summary>
    public readonly record struct SpriteLayout(IntRect Source, LevelBounds Dest, LevelBounds Hit);

    /// <summary>
    /// Pure placement math. The game renders HD atlas art on a world scaled up by
    /// <see cref="MapScale"/> (GameScene.Show uses mapScale = 3f), so in level space a sprite's size is
    /// atlasPixels * scale / MapScale, where scale is the per-object scale the game applies to that
    /// sprite. Objects are center-anchored on their untrimmed sourceSize.
    /// </summary>
    public static class SpritePlacement
    {
        public const double MapScale = 3.0;

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
