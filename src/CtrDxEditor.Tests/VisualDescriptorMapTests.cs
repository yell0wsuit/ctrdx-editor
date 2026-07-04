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
            System.Collections.Generic.IReadOnlyCollection<string> required =
                VisualDescriptorMap.RequiredFiles(".webp");
            Assert.Contains("images/obj_bubble.webp", required);
            Assert.Contains("images/obj_bubble.json", required);
        }
    }
}
