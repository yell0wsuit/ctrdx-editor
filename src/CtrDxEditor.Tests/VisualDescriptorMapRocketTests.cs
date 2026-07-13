using System.Linq;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the rocket sprite descriptors.</summary>
    public class VisualDescriptorMapRocketTests
    {
        [Fact]
        public void RocketBodyUsesQuad10AtGameScale()
        {
            VisualDescriptor? rocket = VisualDescriptorMap.For("rocket");
            Assert.NotNull(rocket);
            Assert.Equal(0.7, rocket.Scale);
            Assert.Single(rocket.Layers);
            Assert.Equal(10, rocket.Layers[0].Quad);
        }

        [Fact]
        public void RocketLauncherUsesQuad0()
        {
            VisualDescriptor? launcher = VisualDescriptorMap.For("rocket_launcher");
            Assert.NotNull(launcher);
            Assert.Equal(0, launcher.Layers[0].Quad);
        }

        [Fact]
        public void RocketAtlasIsRequired()
        {
            Assert.Contains("images/obj_rocket.json", VisualDescriptorMap.RequiredFiles(".png"));
            Assert.Contains("images/obj_rocket.png", VisualDescriptorMap.RequiredFiles(".png"));
        }
    }
}
