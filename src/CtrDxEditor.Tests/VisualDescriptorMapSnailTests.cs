using System.Linq;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the snail sprite descriptor.</summary>
    public class VisualDescriptorMapSnailTests
    {
        /// <summary>
        /// Snail.InitWithTexture leaves the snail in SNAIL_STATE_INACTIVE, which draws the sleepy eyes
        /// (quad 2) from backContainer behind the shell (quad 8).
        /// </summary>
        [Fact]
        public void SnailDrawsSleepyEyesBehindTheShell()
        {
            VisualDescriptor? snail = VisualDescriptorMap.For("load");
            Assert.NotNull(snail);
            Assert.Equal([2, 8], snail.Layers.Select(l => l.Quad));
            Assert.All(snail.Layers, l => Assert.Equal("images/obj_snail", l.AtlasImageBasePath));
        }

        /// <summary>The spawn and pulse scale timelines are runtime-only, so the sprite draws unscaled.</summary>
        [Fact]
        public void SnailDrawsAtFullScale()
        {
            Assert.Equal(1.0, VisualDescriptorMap.For("load")!.Scale);
        }

        /// <summary>Declaring the layers is what pulls obj_snail into the downloaded content set.</summary>
        [Fact]
        public void SnailAtlasIsRequired()
        {
            Assert.Contains("images/obj_snail.json", VisualDescriptorMap.RequiredFiles(".png"));
            Assert.Contains("images/obj_snail.png", VisualDescriptorMap.RequiredFiles(".png"));
        }
    }
}
