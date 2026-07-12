using System;

namespace CtrDxEditor.Controls
{
    /// <summary>Provides pure geometry calculations for marquee scrolling.</summary>
    public static class MarqueeMath
    {
        /// <summary>Calculates how many pixels the text exceeds its viewport.</summary>
        /// <param name="textWidth">The measured width of the text in pixels.</param>
        /// <param name="availableWidth">The available viewport width in pixels.</param>
        /// <returns>The excess width in pixels, clamped to zero when the text fits.</returns>
        public static double Overflow(double textWidth, double availableWidth)
        {
            return Math.Max(0, textWidth - availableWidth);
        }
    }
}
