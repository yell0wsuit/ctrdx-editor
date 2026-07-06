using System;
using System.Globalization;

using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Input;

namespace CtrDxEditor.Converters
{
    /// <summary>Converters for property field enabled/disabled cursor affordances.</summary>
    public static class PropertyFieldStateConverters
    {
        /// <summary>Maps enabled state to the cursor shown over property rows and editors.</summary>
        public static readonly IValueConverter Cursor = new CursorConverter();

        private static bool Enabled(object? value)
        {
            return value is not bool enabled || enabled;
        }

        private sealed class CursorConverter : IValueConverter
        {
            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return Enabled(value)
                    ? new Cursor(StandardCursorType.Arrow)
                    : new Cursor(StandardCursorType.No);
            }

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return AvaloniaProperty.UnsetValue;
            }
        }

    }
}
