using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Atlas
{
    /// <summary>
    /// One TexturePacker frame. <paramref name="Frame"/> is the sub-rect in the atlas PNG.
    /// <paramref name="SpriteSource"/>.X/Y is where the trimmed pixels sit inside the original
    /// untrimmed sprite (<paramref name="SourceSize"/>). The GUI uses these to undo trimming so the
    /// object's anchor lands on its x/y.
    /// </summary>
    public sealed record AtlasFrame(
        string Filename,
        IntRect Frame,
        IntRect SpriteSource,
        IntSize SourceSize,
        bool Rotated,
        bool Trimmed);
}
