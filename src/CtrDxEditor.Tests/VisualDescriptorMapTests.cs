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
            Assert.Equal("obj_bubble_attached_frame_0000.png", baseLayer.FrameName);
            Assert.Equal("images/obj_bubble.json", baseLayer.AtlasJsonRelPath);

            Assert.Equal(
                [
                    "obj_bubble_attached_frame_0001.png",
                    "obj_bubble_attached_frame_0002.png",
                    "obj_bubble_attached_frame_0003.png",
                ],
                bubble.RandomBackLayers.Select(l => l.FrameName));
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
            Assert.Equal("frame_0056.png", layer.FrameName);
        }

        /// <summary>The additive lit-glow halo is registered as its own quad, separate from the bulb sprite.</summary>
        [Fact]
        public void LightBulbGlowUsesLightQuad()
        {
            VisualDescriptor glow = VisualDescriptorMap.For("lightBulb_glow")!;

            SpriteLayer layer = Assert.Single(glow.Layers);
            Assert.Equal("images/obj_lighter.json", layer.AtlasJsonRelPath);
            Assert.Equal("images/obj_lighter", layer.AtlasImageBasePath);
            Assert.Equal("01_light.png", layer.FrameName);
        }
    }
}
