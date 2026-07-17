using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Atlas
{
    /// <summary>
    /// One TexturePacker frame. <paramref name="Frame"/> is the sub-rect in the atlas PNG.
    /// <paramref name="SpriteSource"/>.X/Y is where the trimmed pixels sit inside the original
    /// untrimmed sprite (<paramref name="SourceSize"/>). The GUI uses these to undo trimming so the
    /// object's anchor lands on its x/y.
    /// </summary>
    /// <param name="Filename">The frame's name in the atlas JSON.</param>
    /// <param name="Frame">The sub-rect within the atlas image.</param>
    /// <param name="SpriteSource">Where the trimmed pixels sit inside the untrimmed sprite.</param>
    /// <param name="SourceSize">The untrimmed sprite size, which the object is center-anchored on.</param>
    /// <param name="Rotated">Whether TexturePacker stored the frame rotated 90 degrees.</param>
    /// <param name="Trimmed">Whether transparent edges were trimmed, making <paramref name="SpriteSource"/> meaningful.</param>
    public sealed record AtlasFrame(
        string Filename,
        IntRect Frame,
        IntRect SpriteSource,
        IntSize SourceSize,
        bool Rotated,
        bool Trimmed);
}
