using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace CtrDxEditor.Converters
{
    /// <summary>
    /// Clamps a dialog's preferred size to what the window actually offers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dialogs are hosted by <c>DialogHost.Avalonia</c>, whose popup host arranges the dialog into a slot
    /// equal to the dialog's own <c>DesiredSize</c>. A panel's <c>DesiredSize</c> is its content size, and
    /// <c>HorizontalAlignment</c> is an arrange-time property that never contributes to it — so
    /// <c>MaxWidth</c> plus <c>Stretch</c> cannot size a dialog here. It only caps, leaving the dialog
    /// shrink-wrapped around its content.
    /// </para>
    /// <para>
    /// Binding the size to the top level's bounds through this converter is what makes the dialog measure
    /// at <c>min(preferred, available)</c>: wide enough to look unchanged on a desktop window, small
    /// enough to fit a phone. The source is the top level rather than the dialog itself, so there is no
    /// measure cycle.
    /// </para>
    /// </remarks>
    public sealed class DialogSizeConverter : IValueConverter
    {
        /// <summary>Shared converter instance for XAML bindings.</summary>
        public static readonly DialogSizeConverter Clamp = new();

        /// <summary>
        /// Space reserved for the dialog's own margin, which sits outside the clamped size. Matches the
        /// <c>Margin="24"</c> every dialog root carries, doubled for both edges.
        /// </summary>
        public const double Inset = 48;

        /// <summary>
        /// Returns <c>min(parameter, value - <see cref="Inset"/>)</c>, or <see cref="double.NaN"/> (auto)
        /// when either side is unusable — an unmeasured top level, or a window too small to hold the
        /// inset. Auto is the safe fallback: the dialog sizes to its content as it would without binding.
        /// </summary>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not double available || double.IsNaN(available) || double.IsInfinity(available))
            {
                return double.NaN;
            }

            if (!TryParsePreferred(parameter, out double preferred))
            {
                return double.NaN;
            }

            double usable = available - Inset;
            return usable <= 0 ? double.NaN : Math.Min(preferred, usable);
        }

        /// <summary>Not supported: a clamped size cannot be mapped back to a window size.</summary>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("DialogSizeConverter is one-way.");
        }

        private static bool TryParsePreferred(object? parameter, out double preferred)
        {
            switch (parameter)
            {
                case double d:
                    preferred = d;
                    break;
                case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed):
                    preferred = parsed;
                    break;
                default:
                    preferred = double.NaN;
                    return false;
            }

            return !double.IsNaN(preferred) && preferred > 0;
        }
    }
}
