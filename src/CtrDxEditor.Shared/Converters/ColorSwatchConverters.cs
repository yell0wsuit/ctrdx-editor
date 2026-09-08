using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.Converters
{
    /// <summary>Converters for the color field's swatch, shown beside its hex/triplet text box.</summary>
    public static class ColorSwatchConverters
    {
        /// <summary>
        /// Maps a color field's raw string value to the swatch brush: the parsed color for a valid
        /// <c>#RRGGBB</c> hex or <c>R,G,B</c> triplet, transparent for anything else (unset, or a value
        /// the user is mid-typing). The swatch keeps its border either way, so an invalid value reads as
        /// an empty box rather than crashing the panel or silently showing a wrong color.
        /// </summary>
        public static readonly IValueConverter Background = new ColorSwatchBrushConverter();

        private sealed class ColorSwatchBrushConverter : IValueConverter
        {
            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return value is string text && TutorialColor.TryParse(text, out TutorialColor color)
                    ? new SolidColorBrush(Color.FromRgb(color.Red, color.Green, color.Blue))
                    : Brushes.Transparent;
            }

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }
    }
}
