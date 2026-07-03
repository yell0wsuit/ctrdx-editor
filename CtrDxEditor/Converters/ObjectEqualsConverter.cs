using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia.Data.Converters;

namespace CtrDxEditor.Converters
{
    /// <summary>True when the two bound values are equal — used to show the lock icon on the locked row.</summary>
    public sealed class ObjectEqualsConverter : IMultiValueConverter
    {
        public static readonly ObjectEqualsConverter Instance = new();

        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            return values.Count == 2 && values[0] is not null && Equals(values[0], values[1]);
        }
    }
}
