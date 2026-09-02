using System;
using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// A tutorial prompt's authored color, in either spelling the game accepts. Mirrors
    /// TutorialEvent.ParseColor/ParseHex/ParseChannels. <see cref="Triplet"/> records which spelling
    /// was authored so <see cref="Format"/> returns it unchanged, keeping a merely-opened level
    /// byte-identical on save; a color the user edits through the picker is normalised to
    /// <c>#RRGGBB</c> by that field, not here.
    /// </summary>
    public readonly record struct TutorialColor(byte Red, byte Green, byte Blue, bool Triplet)
    {
        /// <summary>
        /// Parses exactly <c>#RRGGBB</c> or a comma-separated <c>R,G,B</c> triplet of 0-255 channels,
        /// spaces around a channel allowed. Anything else, including a malformed hex or an
        /// out-of-range channel, fails rather than falling back, so the caller decides the default.
        /// </summary>
        public static bool TryParse(string? value, out TutorialColor color)
        {
            if (value is not null && value.StartsWith('#'))
            {
                return TryParseHex(value, out color);
            }

            return TryParseChannels(value, out color);
        }

        /// <summary>Renders as authored: <c>#RRGGBB</c> for a hex color, <c>R,G,B</c> for a triplet.</summary>
        public string Format()
        {
            return Triplet
                ? $"{Red},{Green},{Blue}"
                : FormatHex(Red, Green, Blue);
        }

        /// <summary>Renders channels as the <c>#RRGGBB</c> spelling, regardless of how they were authored.</summary>
        public static string FormatHex(byte red, byte green, byte blue)
        {
            return $"#{red:X2}{green:X2}{blue:X2}";
        }

        private static bool TryParseHex(string value, out TutorialColor color)
        {
            if (value.Length == 7
                && int.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int packed))
            {
                color = new TutorialColor(
                    (byte)((packed >> 16) & 0xFF),
                    (byte)((packed >> 8) & 0xFF),
                    (byte)(packed & 0xFF),
                    Triplet: false);
                return true;
            }

            color = default;
            return false;
        }

        private static bool TryParseChannels(string? value, out TutorialColor color)
        {
            string[] parts = value?.Split(',') ?? [];
            if (parts.Length == 3
                && TryChannel(parts[0], out byte red)
                && TryChannel(parts[1], out byte green)
                && TryChannel(parts[2], out byte blue))
            {
                color = new TutorialColor(red, green, blue, Triplet: true);
                return true;
            }

            color = default;
            return false;
        }

        private static bool TryChannel(string part, out byte channel)
        {
            if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                && parsed is >= 0 and <= 255)
            {
                channel = (byte)parsed;
                return true;
            }

            channel = 0;
            return false;
        }
    }

    /// <summary>
    /// A tutorial prompt's look: opacity, optional color override, rotation, and the type-setting
    /// multipliers a sign ignores. Mirrors the loader's Parse.
    /// </summary>
    public readonly record struct TutorialLook(
        double Opacity,
        TutorialColor? Color,
        double Angle,
        double Size,
        double LineHeight)
    {
        /// <summary>Reads a prompt's look attributes.</summary>
        public static TutorialLook For(LevelObject o)
        {
            return new TutorialLook(
                UnitInterval(o.GetAttr("opacity"), 1.0),
                ColorOf(o.GetAttr("color")),
                Finite(o.GetAttr("angle"), 0.0),
                Positive(o.GetAttr("size"), 1.0),
                Positive(o.GetAttr("lineHeight"), 1.0));
        }

        private static TutorialColor? ColorOf(string? value)
        {
            return value is not null && TutorialColor.TryParse(value, out TutorialColor color)
                ? color
                : null;
        }

        private static double UnitInterval(string? value, double fallback)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                && parsed is >= 0 and <= 1
                    ? parsed
                    : fallback;
        }

        private static double Finite(string? value, double fallback)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                && double.IsFinite(parsed)
                    ? parsed
                    : fallback;
        }

        private static double Positive(string? value, double fallback)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                && double.IsFinite(parsed)
                && parsed > 0
                    ? parsed
                    : fallback;
        }
    }
}
