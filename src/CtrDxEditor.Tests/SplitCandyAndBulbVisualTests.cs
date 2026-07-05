using System.Linq;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the sprites for split candy halves and the light bulb.</summary>
    public class SplitCandyAndBulbVisualTests
    {
        [Theory]
        [InlineData("candyL")]
        [InlineData("candyR")]
        public void SplitCandyUsesHalfCandySprite(string element)
        {
            VisualDescriptor half = VisualDescriptorMap.For(element)!;

            Assert.Equal(0.71, half.Scale);
            Assert.Equal(
                [element == "candyL" ? "frame_08_part_1.png" : "frame_09_part_2.png"],
                half.Layers.Select(l => l.FrameName));
        }

        [Fact]
        public void LightBulbUsesObjLighterBottleAndTop()
        {
            VisualDescriptor bulb = VisualDescriptorMap.For("lightBulb")!;

            Assert.Equal(
                ["02_bottle.png", "03_top.png"],
                bulb.Layers.Select(l => l.FrameName));
            Assert.All(bulb.Layers, l => Assert.Equal("images/obj_lighter.json", l.AtlasJsonRelPath));
        }

        [Fact]
        public void RequiredFilesIncludeObjLighter()
        {
            System.Collections.Generic.IReadOnlyCollection<string> required = VisualDescriptorMap.RequiredFiles(".webp");
            Assert.Contains("images/obj_lighter.webp", required);
            Assert.Contains("images/obj_lighter.json", required);
        }
    }
}
