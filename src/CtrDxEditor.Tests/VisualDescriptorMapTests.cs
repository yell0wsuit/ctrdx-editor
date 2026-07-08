using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for the built-in visual descriptor set.</summary>
    public class VisualDescriptorMapTests
    {
        /// <summary>
        /// Verifies the bubble draws the game's quad 0 attached frame over one of the three random
        /// attached-outline variants used by LoadBubble.
        /// </summary>
        [Fact]
        public void BubbleHasAttachedBaseAndThreeOutlineVariants()
        {
            VisualDescriptor bubble = VisualDescriptorMap.For("bubble")!;

            SpriteLayer baseLayer = Assert.Single(bubble.Layers);
            Assert.Equal("images/obj_bubble.json", baseLayer.AtlasJsonRelPath);
            Assert.Equal(0, baseLayer.Quad);

            Assert.Equal([1, 2, 3], bubble.RandomBackLayers.Select(l => l.Quad));
        }

        /// <summary>Verifies hook-family descriptors resolve the engine quad positions, not frame names.</summary>
        [Fact]
        public void HookFamilyDescriptorsUseGameQuadIndices()
        {
            Assert.Equal([0, 1], VisualDescriptorMap.For("grab")!.Layers.Select(l => l.Quad));
            Assert.Equal([4, 5], VisualDescriptorMap.For("grab_auto")!.Layers.Select(l => l.Quad));
            Assert.Equal([6, 8, 7], VisualDescriptorMap.For("grab_rail")!.Layers.Select(l => l.Quad));
            Assert.Equal([10], VisualDescriptorMap.For("grab_movable")!.Layers.Select(l => l.Quad));
            Assert.Equal([9], VisualDescriptorMap.For("grab_movable_highlight")!.Layers.Select(l => l.Quad));
        }

        /// <summary>Verifies simple zero-based atlases carry their quad indices in descriptors.</summary>
        [Fact]
        public void SingleAtlasObjectsUseQuadIndices()
        {
            Assert.Equal([0, 1, 2], VisualDescriptorMap.For("grab_gun")!.Layers.Select(l => l.Quad));
            Assert.Equal([0], VisualDescriptorMap.For("grab_spider")!.Layers.Select(l => l.Quad));
            Assert.Equal([3, 4], VisualDescriptorMap.For("grab_suction")!.Layers.Select(l => l.Quad));
            Assert.Equal([1, 2], VisualDescriptorMap.For("grab_suction_kicked")!.Layers.Select(l => l.Quad));
            Assert.Equal([0, 18], VisualDescriptorMap.For("star")!.Layers.Select(l => l.Quad));
            Assert.Equal([20, 19], VisualDescriptorMap.For("star_timed")!.Layers.Select(l => l.Quad));
        }

        /// <summary>Verifies random back layers count toward the files a content bundle must provide.</summary>
        [Fact]
        public void RequiredFilesCoverRandomBackLayerAtlases()
        {
            IReadOnlyCollection<string> required =
                VisualDescriptorMap.RequiredFiles(".webp");
            Assert.Contains("images/obj_bubble.webp", required);
            Assert.Contains("images/obj_bubble.json", required);
        }

        /// <summary>Verifies that gravity switches use the same static quad as the in-game toggle button.</summary>
        [Fact]
        public void GravitySwitchUsesObjStarIdleButtonFrame()
        {
            VisualDescriptor gravitySwitch = VisualDescriptorMap.For("gravitySwitch")!;

            SpriteLayer layer = Assert.Single(gravitySwitch.Layers);
            Assert.Equal("images/obj_star_idle.json", layer.AtlasJsonRelPath);
            Assert.Equal("images/obj_star_idle", layer.AtlasImageBasePath);
            Assert.Equal(21, layer.Quad);
        }

        /// <summary>The additive lit-glow halo is registered as its own quad, separate from the bulb sprite.</summary>
        [Fact]
        public void LightBulbGlowUsesLightQuad()
        {
            VisualDescriptor glow = VisualDescriptorMap.For("lightBulb_glow")!;

            SpriteLayer layer = Assert.Single(glow.Layers);
            Assert.Equal("images/obj_lighter.json", layer.AtlasJsonRelPath);
            Assert.Equal("images/obj_lighter", layer.AtlasImageBasePath);
            Assert.Equal(0, layer.Quad);
        }
    }
}
