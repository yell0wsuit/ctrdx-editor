using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia.Data.Converters;

namespace CtrDxEditor.Converters
{
    /// <summary>Converts scroll activity and pointer-over state into the scrollbar overlay's target opacity.</summary>
    public sealed class ScrollOverlayOpacityConverter : IMultiValueConverter
    {
        /// <summary>Shared converter instance for XAML bindings.</summary>
        public static readonly ScrollOverlayOpacityConverter Instance = new();

        /// <inheritdoc />
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            bool visible = values.Count == 2
                && ((values[0] is bool active && active) || (values[1] is bool over && over));
            return visible ? 1.0 : 0.0;
        }
    }
}
