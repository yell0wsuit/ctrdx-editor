using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia.Data.Converters;

namespace CtrDxEditor.Converters
{
    /// <summary>
    /// A list row is enabled when nothing is locked, or when this row is the locked object — so while a
    /// lock is active every other row is disabled (dimmed and unselectable).
    /// </summary>
    public sealed class LockRowEnabledConverter : IMultiValueConverter
    {
        /// <summary>Shared converter instance for XAML bindings.</summary>
        public static readonly LockRowEnabledConverter Instance = new();

        /// <inheritdoc />
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count != 2)
            {
                return true;
            }

            object? locked = values[1];
            return locked is null || Equals(values[0], locked);
        }
    }
}
