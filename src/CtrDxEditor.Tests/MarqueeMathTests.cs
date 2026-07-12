using CtrDxEditor.Controls;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests overflow geometry used by the palette marquee.</summary>
    public class MarqueeMathTests
    {
        /// <summary>Text narrower than the viewport has no overflow.</summary>
        [Fact]
        public void NoOverflowWhenTextFits()
        {
            Assert.Equal(0d, MarqueeMath.Overflow(textWidth: 80, availableWidth: 120));
        }

        /// <summary>Text exactly as wide as the viewport has no overflow.</summary>
        [Fact]
        public void NoOverflowAtExactFit()
        {
            Assert.Equal(0d, MarqueeMath.Overflow(textWidth: 120, availableWidth: 120));
        }

        /// <summary>Overflow equals the text width beyond the viewport width.</summary>
        [Fact]
        public void OverflowIsTheExcessWidth()
        {
            Assert.Equal(30d, MarqueeMath.Overflow(textWidth: 150, availableWidth: 120));
        }
    }
}
