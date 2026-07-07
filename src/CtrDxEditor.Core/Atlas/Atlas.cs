using System.Collections.Generic;
using System.Linq;

namespace CtrDxEditor.Core.Atlas
{
    /// <summary>In-memory lookup table for TexturePacker atlas frames.</summary>
    public sealed class Atlas(IReadOnlyList<AtlasFrame> frames)
    {
        /// <summary>All frames loaded from the atlas JSON, in source order.</summary>
        public IReadOnlyList<AtlasFrame> Frames { get; } = frames;

        /// <summary>Finds a frame by its TexturePacker filename.</summary>
        public AtlasFrame? Find(string filename)
        {
            return Frames.FirstOrDefault(f => f.Filename == filename);
        }

        /// <summary>
        /// Returns the frame at the given zero-based position (the engine's "quad" index), or null when
        /// out of range. Used where atlases share a frame order but not frame names (e.g. candy skins,
        /// which the game addresses purely by quad index).
        /// </summary>
        public AtlasFrame? At(int index)
        {
            return index >= 0 && index < Frames.Count ? Frames[index] : null;
        }
    }
}
