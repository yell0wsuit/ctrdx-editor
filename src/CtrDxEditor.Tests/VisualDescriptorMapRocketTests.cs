using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the rocket sprite descriptors.</summary>
    public class VisualDescriptorMapRocketTests
    {
        /// <summary>The rocket body draws as a single quad-10 layer at the 0.7 scale LoadRocket applies in game.</summary>
        [Fact]
        public void RocketBodyUsesQuad10AtGameScale()
        {
            VisualDescriptor? rocket = VisualDescriptorMap.For("rocket");
            Assert.NotNull(rocket);
            Assert.Equal(0.7, rocket.Scale);
            _ = Assert.Single(rocket.Layers);
            Assert.Equal(10, rocket.Layers[0].Quad);
        }

        /// <summary>The launcher marker keeps quad 0 at full scale, because only the body carries the 0.7 shrink.</summary>
        [Fact]
        public void RocketLauncherUsesQuad0AtFullScale()
        {
            VisualDescriptor? launcher = VisualDescriptorMap.For("rocket_launcher");
            Assert.NotNull(launcher);
            Assert.Equal(0, launcher.Layers[0].Quad);
            // LoadRocket leaves the marker Image at the default scale 1.0 (only the body is 0.7).
            Assert.Equal(1.0, launcher.Scale);
        }

        /// <summary>
        /// Whether the rocket assets and atlas are present
        /// </summary>
        [Fact]
        public void RocketAtlasIsRequired()
        {
            Assert.Contains("images/obj_rocket.json", VisualDescriptorMap.RequiredFiles(".png"));
            Assert.Contains("images/obj_rocket.png", VisualDescriptorMap.RequiredFiles(".png"));
        }
    }
}
