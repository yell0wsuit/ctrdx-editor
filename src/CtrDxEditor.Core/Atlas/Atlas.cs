using System.Collections.Generic;

namespace CtrDxEditor.Core.Atlas
{
    /// <summary>In-memory lookup table for TexturePacker atlas frames.</summary>
    public sealed class Atlas(IReadOnlyList<AtlasFrame> frames)
    {
        /// <summary>All frames loaded from the atlas JSON, in source order.</summary>
        public IReadOnlyList<AtlasFrame> Frames { get; } = frames;

        /// <summary>
        /// Returns the frame at the given zero-based position (the engine's "quad" index), or null when
        /// out of range.
        /// </summary>
        public AtlasFrame? At(int index)
        {
            return index >= 0 && index < Frames.Count ? Frames[index] : null;
        }
    }
}
