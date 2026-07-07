using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the sprites for split candy halves and the light bulb.</summary>
    public class SplitCandyAndBulbVisualTests
    {
        /// <summary>
        /// Each split-candy half uses the half-candy sprite at 0.71 scale, addressed by quad index
        /// (candyL = 8, candyR = 9) so it resolves across candy skins with differing frame names.
        /// </summary>
        [Theory]
        [InlineData("candyL", 8)]
        [InlineData("candyR", 9)]
        public void SplitCandyUsesHalfCandySprite(string element, int expectedQuad)
        {
            VisualDescriptor half = VisualDescriptorMap.For(element)!;

            Assert.Equal(0.71, half.Scale);
            Assert.Equal([expectedQuad], half.Layers.Select(l => l.Quad));
        }

        /// <summary>The light bulb draws the obj_lighter bottle and top layers.</summary>
        [Fact]
        public void LightBulbUsesObjLighterBottleAndTop()
        {
            VisualDescriptor bulb = VisualDescriptorMap.For("lightBulb")!;

            Assert.Equal(
                ["02_bottle.png", "03_top.png"],
                bulb.Layers.Select(l => l.FrameName));
            Assert.All(bulb.Layers, l => Assert.Equal("images/obj_lighter.json", l.AtlasJsonRelPath));
        }

        /// <summary>The bulb's obj_lighter image and atlas are listed among the required content files.</summary>
        [Fact]
        public void RequiredFilesIncludeObjLighter()
        {
            IReadOnlyCollection<string> required = VisualDescriptorMap.RequiredFiles(".webp");
            Assert.Contains("images/obj_lighter.webp", required);
            Assert.Contains("images/obj_lighter.json", required);
        }
    }
}
