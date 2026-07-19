using System;
using System.Globalization;

using Avalonia.Data.Converters;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Localization;

namespace CtrDxEditor.Converters
{
    /// <summary>
    /// The layer-tree label for an object: its localized name, suffixed with "(locale)" for objects
    /// that carry a locale attribute (tutorial text/icons) so per-language variants are distinguishable.
    /// </summary>
    public sealed class ObjectRowLabelConverter : IValueConverter
    {
        /// <summary>Shared converter instance for XAML bindings.</summary>
        public static readonly ObjectRowLabelConverter Instance = new();

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not LevelObject obj)
            {
                return value ?? "";
            }

            string name = Localizer.ObjectName(obj.Type);
            return obj.GetAttr("locale") is { } locale ? $"{name} ({locale})" : name;
        }

        /// <inheritdoc />
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
