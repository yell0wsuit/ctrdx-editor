using System.Linq;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests ant-conveyor sprite atlas registration.</summary>
    public class VisualDescriptorMapAntTests
    {
        /// <summary>The placeable object exposes a stable first-frame thumbnail and requires its atlas files.</summary>
        [Fact]
        public void AntAtlasFramesAreAvailableToRenderer()
        {
            VisualDescriptor ants = VisualDescriptorMap.For("ants")!;

            Assert.Equal([0], ants.Layers.Select(l => l.Quad));
            Assert.Contains("images/obj_ant.json", VisualDescriptorMap.RequiredFiles(".png"));
            Assert.Contains("images/obj_ant.png", VisualDescriptorMap.RequiredFiles(".png"));
        }

        /// <summary>The renderer descriptor provides six walk frames followed by the endpoint-hole frame.</summary>
        [Fact]
        public void AntPartsExposeWalkFramesAndHole()
        {
            Assert.Equal(
                Enumerable.Range(0, 7),
                VisualDescriptorMap.For("ant_parts")!.Layers.Select(l => l.Quad));
        }
    }
}
