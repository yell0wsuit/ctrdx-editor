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
    }
}
