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
        public void SplitCandyReusesCandySprite(string element)
        {
            VisualDescriptor candy = VisualDescriptorMap.For("candy")!;
            VisualDescriptor half = VisualDescriptorMap.For(element)!;

            Assert.Equal(candy.Scale, half.Scale);
            Assert.Equal(
                candy.Layers.Select(l => l.FrameName),
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
