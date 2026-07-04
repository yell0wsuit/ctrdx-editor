using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the per-instance decorative variant choice (the game's random bubble outline).</summary>
    public class SpriteVariantPickerTests
    {
        /// <summary>Verifies a placed object keeps the same variant across repaints (no flicker).</summary>
        [Fact]
        public void SameElementAlwaysGetsTheSameIndex()
        {
            XElement element = new("bubble");
            int first = SpriteVariantPicker.Pick(element, count: 3);
            Assert.All(Enumerable.Range(0, 20), _ => Assert.Equal(first, SpriteVariantPicker.Pick(element, count: 3)));
        }

        /// <summary>Verifies picked indices stay within the variant list.</summary>
        [Fact]
        public void IndexIsAlwaysInRange()
        {
            Assert.All(
                Enumerable.Range(0, 100).Select(_ => SpriteVariantPicker.Pick(new XElement("bubble"), count: 3)),
                i => Assert.InRange(i, 0, 2));
        }

        /// <summary>Verifies choices actually vary between instances (mirrors the game's RND_RANGE).</summary>
        [Fact]
        public void DifferentElementsGetVariedIndices()
        {
            // 100 draws from 3 variants land on a single index with probability ~3 * (1/3)^100 ≈ 0.
            int[] picks = [.. Enumerable.Range(0, 100).Select(_ => SpriteVariantPicker.Pick(new XElement("bubble"), count: 3))];
            Assert.True(picks.Distinct().Count() > 1);
        }
    }
}
