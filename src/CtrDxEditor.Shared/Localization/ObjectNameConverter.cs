using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace CtrDxEditor.Localization
{
    /// <summary>Converts a level element id (LevelObject.Type) to its localized display name.</summary>
    public sealed class ObjectNameConverter : IValueConverter
    {
        /// <summary>Shared converter instance for XAML bindings.</summary>
        public static readonly ObjectNameConverter Instance = new();

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string element ? Localizer.ObjectName(element) : value ?? "";
        }

        /// <inheritdoc />
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
