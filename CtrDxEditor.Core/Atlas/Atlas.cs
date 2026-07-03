using System.Collections.Generic;
using System.Linq;

namespace CtrDxEditor.Core.Atlas
{
    public sealed class Atlas(IReadOnlyList<AtlasFrame> frames)
    {
        public IReadOnlyList<AtlasFrame> Frames { get; } = frames;

        public AtlasFrame? Find(string filename)
        {
            return Frames.FirstOrDefault(f => f.Filename == filename);
        }
    }
}
