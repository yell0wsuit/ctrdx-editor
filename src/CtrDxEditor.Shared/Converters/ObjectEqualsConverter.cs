using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia.Data.Converters;

namespace CtrDxEditor.Converters
{
    /// <summary>True when the two bound values are equal; optional Invert flips the result.</summary>
    public sealed class ObjectEqualsConverter : IMultiValueConverter
    {
        /// <summary>Shared converter instance for XAML bindings.</summary>
        public static readonly ObjectEqualsConverter Instance = new();

        /// <inheritdoc />
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            bool equal = values.Count == 2 && values[0] is not null && Equals(values[0], values[1]);
            return string.Equals(parameter as string, "Invert", StringComparison.Ordinal) ? !equal : equal;
        }
    }
}
