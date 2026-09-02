using System;
using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// A tutorial prompt's delay and fade envelope. Mirrors the loader's parse plus the timeline
    /// TutorialPromptLoader.BuildEnvelope constructs, evaluated in closed form so preview needs no
    /// timeline runtime.
    /// </summary>
    public readonly record struct TutorialTiming(
        double Delay,
        double FadeIn,
        double Hold,
        double FadeOut,
        int Repeat)
    {
        /// <summary>Authored hold meaning "stay up until the level ends".</summary>
        public const double ForeverHold = -1.0;

        /// <summary>Authored pass count meaning "repeat until the level ends".</summary>
        public const int ForeverRepeat = -1;

        /// <summary>Reads a prompt's timing, defaulting the hold by element kind as the loader does.</summary>
        public static TutorialTiming For(LevelObject o)
        {
            double defaultHold = TutorialObject.IsText(o.Type) ? 5.0 : 5.2;
            return new TutorialTiming(
                NonNegative(o.GetAttr("delay"), 0.0),
                NonNegative(o.GetAttr("fadeIn"), 1.0),
                HoldOf(o.GetAttr("duration"), defaultHold),
                NonNegative(o.GetAttr("fadeOut"), 0.5),
                RepeatOf(o.GetAttr("repeat")));
        }

        /// <summary>Whether the prompt holds at peak instead of fading out.</summary>
        public bool HoldsForever => Hold == ForeverHold;

        /// <summary>Whether the prompt loops its pass for the level's lifetime.</summary>
        public bool RepeatsForever => Repeat == ForeverRepeat;

        /// <summary>Seconds one pass occupies, or infinity when the prompt holds forever.</summary>
        public double PassSeconds => HoldsForever
            ? double.PositiveInfinity
            : FadeIn + Hold + FadeOut;

        /// <summary>Seconds the whole prompt occupies, or null when it never ends.</summary>
        public double? TotalSeconds => HoldsForever || RepeatsForever
            ? null
            : Delay + (PassSeconds * Repeat);

        /// <summary>The envelope's alpha at an elapsed time, with the prompt firing at zero.</summary>
        public double AlphaAt(double seconds)
        {
            double elapsed = seconds - Delay;
            if (elapsed < 0)
            {
                return 0;
            }

            if (HoldsForever)
            {
                return FadeIn <= 0 || elapsed >= FadeIn ? 1 : elapsed / FadeIn;
            }

            double pass = PassSeconds;
            if (pass <= 0)
            {
                return 0;
            }

            if (!RepeatsForever && elapsed >= pass * Repeat)
            {
                return 0;
            }

            double inPass = elapsed % pass;
            if (inPass < FadeIn)
            {
                return FadeIn <= 0 ? 1 : inPass / FadeIn;
            }

            if (inPass < FadeIn + Hold)
            {
                return 1;
            }

            return FadeOut <= 0 ? 0 : 1 - ((inPass - FadeIn - Hold) / FadeOut);
        }

        private static double NonNegative(string? value, double fallback)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                && double.IsFinite(parsed)
                && parsed >= 0
                    ? parsed
                    : fallback;
        }

        private static double HoldOf(string? value, double fallback)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                && parsed == ForeverHold
                    ? ForeverHold
                    : NonNegative(value, fallback);
        }

        private static int RepeatOf(string? value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                && (parsed > 0 || parsed == ForeverRepeat)
                    ? parsed
                    : 1;
        }
    }
}
