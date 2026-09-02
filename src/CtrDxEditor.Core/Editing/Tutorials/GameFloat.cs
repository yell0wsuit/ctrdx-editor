using System.Globalization;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Float-precision attribute parsing shared by the tutorial types' float-precision comparisons
    /// (<see cref="TutorialMotion.TravelSecondsAtGameFloatPrecision"/> and
    /// <see cref="TutorialTiming.PassSecondsAtGameFloatPrecision"/>). Mirrors the strict parse the
    /// game itself applies in TutorialPromptLoader.Parse and TutorialValues: a missing attribute uses
    /// the fallback, but a present one must parse as a finite float or the whole read fails - unlike
    /// the lenient double-precision parsing these types also expose for the properties panel and
    /// preview, which falls back to the default on bad input instead of failing.
    /// </summary>
    internal static class GameFloat
    {
        /// <summary>A missing attribute uses <paramref name="fallback"/>; a present one must be finite.</summary>
        internal static bool TryOptional(string? value, float fallback, out float parsed)
        {
            if (value is null)
            {
                parsed = fallback;
                return true;
            }

            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                && float.IsFinite(parsed);
        }

        /// <summary>As <see cref="TryOptional"/>, additionally requiring the value be zero or more.</summary>
        internal static bool TryNonNegative(string? value, float fallback, out float parsed)
        {
            return TryOptional(value, fallback, out parsed) && parsed >= 0f;
        }

        /// <summary>As <see cref="TryOptional"/>, additionally requiring the value be strictly positive.</summary>
        internal static bool TryPositive(string? value, float fallback, out float parsed)
        {
            return TryOptional(value, fallback, out parsed) && parsed > 0f;
        }

        /// <summary>Parses one path pair component: an empty part is zero, matching the game's Coordinate.</summary>
        internal static bool TryPathComponent(string value, out float parsed)
        {
            if (value.Length == 0)
            {
                parsed = 0f;
                return true;
            }

            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                && float.IsFinite(parsed);
        }
    }
}
