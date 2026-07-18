using System.Linq;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the bamboo-tube (pipe) sprite descriptor.</summary>
    public class VisualDescriptorMapBambooTubeTests
    {
        /// <summary>
        /// BambooTube composites the core (quad 0) with the back shell (quad 1) and front shell
        /// (quad 2), drawn back-to-front, matching SetupBambooShellSprites and the core draw quad.
        /// </summary>
        [Fact]
        public void BambooTubeCompositesCoreAndBothShells()
        {
            VisualDescriptor? pipe = VisualDescriptorMap.For("pipe");
            Assert.NotNull(pipe);
            Assert.Equal([0, 1, 2], pipe.Layers.Select(l => l.Quad));
            Assert.All(pipe.Layers, l => Assert.Equal("images/obj_bamboo_tube", l.AtlasImageBasePath));
        }

        /// <summary>The game applies a 0.9 scale to the tube (scaleX/scaleY in InitWithPositionAngle).</summary>
        [Fact]
        public void BambooTubeDrawsAtNinetyPercentScale()
        {
            Assert.Equal(0.9, VisualDescriptorMap.For("pipe")!.Scale);
        }

        /// <summary>Declaring the layers is what pulls obj_bamboo_tube into the downloaded content set.</summary>
        [Fact]
        public void BambooTubeAtlasIsRequired()
        {
            Assert.Contains("images/obj_bamboo_tube.json", VisualDescriptorMap.RequiredFiles(".png"));
            Assert.Contains("images/obj_bamboo_tube.png", VisualDescriptorMap.RequiredFiles(".png"));
        }
    }
}
