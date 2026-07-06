using System;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Per-pixel math for the light-bulb glow's additive blend. The game builds
    /// obj_lighter.png premultiplied (content.mgcb PremultiplyAlpha=True), tints the glow white @ 0.6 alpha
    /// (LightBulb.cs, and 0.6f rounds to exactly 153/255), and draws it through a BasicEffect that
    /// premultiplies diffuse by alpha, so the shader emits src = (p * 0.6, a * 0.6). Its blendingMode=2 maps
    /// to GL_SRC_ALPHA/GL_ONE (BlendParams.cs), which multiplies src.rgb by src.a once more before adding:
    /// each texel contributes p * a * 0.36 to the framebuffer. Baking that product into the texture lets a
    /// plain additive (Plus) draw with an opaque paint reproduce the game's glow exactly.
    /// </summary>
    public static class GlowBlend
    {
        /// <summary>The game's 0.6 glow alpha, applied twice: once by BasicEffect, once by GL_SRC_ALPHA.</summary>
        public const double AdditiveFactor = 0.6 * 0.6;

        /// <summary>
        /// The additive framebuffer contribution of one premultiplied channel value under the game's glow
        /// blend: <c>premul * (alpha/255) * 0.36</c>, rounded like a GPU framebuffer write.
        /// </summary>
        public static byte BakeChannel(byte premul, byte alpha)
        {
            return (byte)Math.Round(premul * (alpha / 255.0) * AdditiveFactor, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Rewrites premultiplied BGRA pixels in place so each pixel holds its additive glow contribution.
        /// Color channels become <c>p * a * 0.36</c>; alpha becomes <c>a * a * 0.36</c> (the game's
        /// GL_SRC_ALPHA/GL_ONE alpha write), which also keeps the buffer valid premultiplied color.
        /// </summary>
        public static void BakeBgraInPlace(Span<byte> bgra)
        {
            for (int i = 0; i + 3 < bgra.Length; i += 4)
            {
                byte alpha = bgra[i + 3];
                bgra[i] = BakeChannel(bgra[i], alpha);
                bgra[i + 1] = BakeChannel(bgra[i + 1], alpha);
                bgra[i + 2] = BakeChannel(bgra[i + 2], alpha);
                bgra[i + 3] = BakeChannel(alpha, alpha);
            }
        }
    }
}
