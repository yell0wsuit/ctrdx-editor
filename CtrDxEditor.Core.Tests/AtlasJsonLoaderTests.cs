using System.Collections.Generic;

using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    public class AtlasJsonLoaderTests
    {
        private const string SampleJson = /*lang=json,strict*/ """
    {
        "frames": [
            {
                "filename": "obj_hook_01_frame_0000.png",
                "frame": { "x": 921, "y": 1, "w": 127, "h": 128 },
                "rotated": false,
                "trimmed": true,
                "spriteSourceSize": { "x": 78, "y": 76, "w": 127, "h": 128 },
                "sourceSize": { "w": 276, "h": 276 }
            }
        ]
    }
    """;

        [Fact]
        public void ParseFrames_reads_rect_trim_and_source_size()
        {
            IReadOnlyList<AtlasFrame> frames = AtlasJsonLoader.ParseFrames(SampleJson);

            AtlasFrame f = Assert.Single(frames);
            Assert.Equal("obj_hook_01_frame_0000.png", f.Filename);
            Assert.Equal(new IntRect(921, 1, 127, 128), f.Frame);
            Assert.Equal(new IntRect(78, 76, 127, 128), f.SpriteSource);
            Assert.Equal(new IntSize(276, 276), f.SourceSize);
            Assert.True(f.Trimmed);
            Assert.False(f.Rotated);
        }

        [Fact]
        public void ParseFrames_on_empty_frames_returns_empty()
        {
            Assert.Empty(AtlasJsonLoader.ParseFrames(/*lang=json,strict*/ """{ "frames": [] }"""));
        }
    }
}
