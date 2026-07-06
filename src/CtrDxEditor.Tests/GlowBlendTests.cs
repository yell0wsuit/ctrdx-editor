using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Tests the 1:1 light-bulb glow additive bake. The game draws the premultiplied glow texture through
    /// a BasicEffect with white @ 0.6 alpha (which premultiplies diffuse by alpha, giving src = (p*0.6, a*0.6))
    /// and blends with GL_SRC_ALPHA/GL_ONE, so each pixel adds p * a * 0.36 to the framebuffer.
    /// </summary>
    public class GlowBlendTests
    {
        /// <summary>The additive factor is the game's 0.6 glow alpha squared (153/255 is exactly 0.6).</summary>
        [Fact]
        public void AdditiveFactorIsGlowAlphaSquared()
        {
            Assert.Equal(0.36, GlowBlend.AdditiveFactor, 10);
        }

        /// <summary>A fully transparent pixel contributes nothing.</summary>
        [Fact]
        public void TransparentPixelBakesToZero()
        {
            Assert.Equal(0, GlowBlend.BakeChannel(0, 0));
        }

        /// <summary>An opaque white pixel adds 0.36 of full brightness (255 * 0.36 = 91.8 -> 92).</summary>
        [Fact]
        public void OpaqueWhitePixelBakesToThirtySixPercent()
        {
            Assert.Equal(92, GlowBlend.BakeChannel(255, 255));
        }

        /// <summary>The contribution is quadratic in alpha: half-alpha white adds a quarter of the opaque amount.</summary>
        [Fact]
        public void HalfAlphaWhiteBakesQuadratically()
        {
            // Premultiplied half-alpha white: p = 128, a = 128 -> 128 * (128/255) * 0.36 = 23.13 -> 23.
            Assert.Equal(23, GlowBlend.BakeChannel(128, 128));
        }

        /// <summary>Baking a BGRA buffer scales every color channel by alpha * 0.36 and squares the alpha itself.</summary>
        [Fact]
        public void BakeBgraInPlaceTransformsEveryPixel()
        {
            // Pixel 1: b=50 g=100 r=150 a=200; pixel 2: opaque white.
            byte[] bgra = [50, 100, 150, 200, 255, 255, 255, 255];
            GlowBlend.BakeBgraInPlace(bgra);
            Assert.Equal(new byte[] { 14, 28, 42, 56, 92, 92, 92, 92 }, bgra);
        }

        /// <summary>Baked pixels stay valid premultiplied colors (channels never exceed the baked alpha).</summary>
        [Fact]
        public void BakePreservesPremultipliedInvariant()
        {
            for (int a = 0; a <= 255; a++)
            {
                Assert.True(GlowBlend.BakeChannel((byte)a, (byte)a) >= GlowBlend.BakeChannel((byte)(a * 3 / 4), (byte)a));
            }
        }
    }
}
