using System;

namespace CtrDxEditor.Controls
{
    /// <summary>Provides pure geometry calculations for marquee scrolling.</summary>
    public static class MarqueeMath
    {
        /// <summary>Default marquee travel speed in pixels per second.</summary>
        public const double DefaultSpeed = 40;

        /// <summary>Default dwell at each readable marquee endpoint, in seconds.</summary>
        public const double DefaultPauseSeconds = 1.5;

        /// <summary>Calculates how many pixels the text exceeds its viewport.</summary>
        /// <param name="textWidth">The measured width of the text in pixels.</param>
        /// <param name="availableWidth">The available viewport width in pixels.</param>
        /// <returns>The excess width in pixels, clamped to zero when the text fits.</returns>
        public static double Overflow(double textWidth, double availableWidth)
        {
            return Math.Max(0, textWidth - availableWidth);
        }

        /// <summary>Calculates a linear back-and-forth offset without using Avalonia's transform animator.</summary>
        /// <param name="overflow">Maximum horizontal travel in pixels.</param>
        /// <param name="elapsedSeconds">Elapsed animation time in seconds.</param>
        /// <param name="speed">Target travel speed in pixels per second.</param>
        /// <param name="minimumLegSeconds">Minimum duration of each one-way leg.</param>
        /// <param name="pauseSeconds">Time spent at each endpoint before moving again.</param>
        /// <returns>An offset between zero and negative <paramref name="overflow"/>.</returns>
        public static double BounceOffset(
            double overflow,
            double elapsedSeconds,
            double speed,
            double minimumLegSeconds,
            double pauseSeconds)
        {
            if (overflow <= 0 || elapsedSeconds <= 0 || speed <= 0)
            {
                return 0;
            }

            double legSeconds = Math.Max(overflow / speed, minimumLegSeconds);
            double pause = Math.Max(0, pauseSeconds);
            double phase = elapsedSeconds % ((2 * legSeconds) + (2 * pause));
            if (phase <= pause)
            {
                return 0;
            }

            phase -= pause;
            if (phase <= legSeconds)
            {
                return -overflow * (phase / legSeconds);
            }

            phase -= legSeconds;
            if (phase <= pause)
            {
                return -overflow;
            }

            phase -= pause;
            return -overflow * (1 - (phase / legSeconds));
        }
    }
}
