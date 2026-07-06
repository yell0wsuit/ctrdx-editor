using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the 1:1 light-bulb glow sizing (game's 1.5x multiplier + aspect-preserving height).</summary>
    public class GlowQuadTests
    {
        // Glow quad 01_light.png is 405x425 (obj_lighter atlas). litRadius 50 -> half-width 75.
        /// <summary>The glow's half-width is litRadius times the game's 1.5x visual multiplier.</summary>
        [Fact]
        public void WidthIsLitRadiusTimesOneAndAHalf()
        {
            (double halfW, _) = GlowQuad.DestRadii(50, 405, 425);
            Assert.Equal(75.0, halfW);
        }

        /// <summary>The glow's half-height scales with the source frame's aspect ratio, so the texture is never distorted.</summary>
        [Fact]
        public void HeightPreservesQuadAspectRatio()
        {
            (double halfW, double halfH) = GlowQuad.DestRadii(50, 405, 425);
            Assert.Equal(halfW * 425.0 / 405.0, halfH, 6);
        }

        /// <summary>A zero-width source frame falls back to a square glow box instead of dividing by zero.</summary>
        [Fact]
        public void ZeroWidthFallsBackToSquare()
        {
            (double halfW, double halfH) = GlowQuad.DestRadii(50, 0, 425);
            Assert.Equal(halfW, halfH);
        }
    }
}
