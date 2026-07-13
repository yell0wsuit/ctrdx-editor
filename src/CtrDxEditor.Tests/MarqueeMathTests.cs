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

        /// <summary>Small overflows honor the minimum leg duration instead of flickering quickly.</summary>
        [Fact]
        public void BounceOffsetUsesMinimumLegDuration()
        {
            Assert.Equal(-5, MarqueeMath.BounceOffset(
                overflow: 10,
                elapsedSeconds: 0.3,
                speed: 40,
                minimumLegSeconds: 0.6,
                pauseSeconds: 0), 6);
        }

        /// <summary>The label pauses at each readable endpoint before reversing direction.</summary>
        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(1.4, 0.0)]
        [InlineData(1.5, 0.0)]
        [InlineData(2.0, -20.0)]
        [InlineData(2.5, -40.0)]
        [InlineData(3.9, -40.0)]
        [InlineData(4.0, -40.0)]
        [InlineData(4.5, -20.0)]
        [InlineData(5.0, 0.0)]
        public void BounceOffsetPausesAtBothEnds(double elapsedSeconds, double expected)
        {
            Assert.Equal(expected, MarqueeMath.BounceOffset(
                overflow: 40,
                elapsedSeconds,
                speed: 40,
                minimumLegSeconds: 0.6,
                pauseSeconds: MarqueeMath.DefaultPauseSeconds), 6);
        }

        /// <summary>The UI dwell gives users 1.5 seconds to read each endpoint.</summary>
        [Fact]
        public void DefaultEndpointPauseIsOneAndAHalfSeconds()
        {
            Assert.Equal(1.5, MarqueeMath.DefaultPauseSeconds);
        }

    }
}
