using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for rope skin palettes and shading.</summary>
    public class RopePaletteTests
    {
        /// <summary>Skin 0 base color is the game's default brown primary.</summary>
        [Fact]
        public void DefaultSkinBaseIsBrown()
        {
            RopeDrawColors c = RopePalette.GetDrawColors(0);

            Assert.Equal(0.475, c.Base1.R, precision: 3);
            Assert.Equal(0.305, c.Base1.G, precision: 3);
            Assert.Equal(0.185, c.Base1.B, precision: 3);
        }

        /// <summary>Out-of-range skin indices fall back to skin 0.</summary>
        [Fact]
        public void OutOfRangeSkinFallsBackToDefault()
        {
            RopeDrawColors fallback = RopePalette.GetDrawColors(99);
            RopeDrawColors zero = RopePalette.GetDrawColors(0);

            Assert.Equal(zero, fallback);
        }

        /// <summary>The default skin shade is darker than its base (produces the ramp).</summary>
        [Fact]
        public void DefaultSkinShadeIsDarkerThanBase()
        {
            RopeDrawColors c = RopePalette.GetDrawColors(0);

            Assert.True(c.Shade1.R < c.Base1.R, "shade should be darker than base");
        }

        /// <summary>A custom skin has shade equal to base (no ramp, pure alternation).</summary>
        [Fact]
        public void CustomSkinShadeEqualsBase()
        {
            // Skin 2 (teal) primary is (0.404, 0.612, 0.635); dark factor is 1.0.
            RopeDrawColors c = RopePalette.GetDrawColors(2);

            Assert.Equal(0.404, c.Base1.R, precision: 3);
            Assert.Equal(c.Base1, c.Shade1);
        }

        /// <summary>Component-wise lerp hits both endpoints and the midpoint.</summary>
        [Fact]
        public void LerpInterpolates()
        {
            RopeRgb a = new(0, 0, 0);
            RopeRgb b = new(1, 0.5, 0.25);

            Assert.Equal(a, RopePalette.Lerp(a, b, 0));
            Assert.Equal(b, RopePalette.Lerp(a, b, 1));

            RopeRgb mid = RopePalette.Lerp(a, b, 0.5);
            Assert.Equal(0.5, mid.R, precision: 6);
            Assert.Equal(0.125, mid.B, precision: 6);
        }

        /// <summary>Skin 0 is the only default skin (drives the outline decision).</summary>
        [Fact]
        public void OnlySkinZeroIsDefault()
        {
            Assert.True(RopePalette.IsDefaultSkin(0));
            Assert.False(RopePalette.IsDefaultSkin(1));
        }
    }
}
