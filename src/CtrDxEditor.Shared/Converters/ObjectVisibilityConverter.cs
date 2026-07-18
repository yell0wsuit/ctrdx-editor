using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia.Data.Converters;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Converters
{
    /// <summary>Converts an object and the editor's hidden-object set into visible/hidden UI state.</summary>
    public sealed class ObjectVisibilityConverter : IMultiValueConverter
    {
        /// <summary>Shared converter instance for XAML bindings.</summary>
        public static readonly ObjectVisibilityConverter Instance = new();

        /// <inheritdoc />
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            bool visible = values.Count == 2
                && values[0] is LevelObject obj
                && values[1] is IReadOnlySet<LevelObject> hidden
                && !hidden.Contains(obj);
            return string.Equals(parameter as string, "Invert", StringComparison.Ordinal) ? !visible : visible;
        }
    }
}
