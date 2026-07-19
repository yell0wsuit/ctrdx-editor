using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia.Data.Converters;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Converters
{
    /// <summary>
    /// Whether an object-row eye toggle is interactive. Objects hidden because they belong to another
    /// locale are governed by the language picker, so their eye is disabled; everything else is enabled.
    /// Inputs: [0] the object, [1] the current display locale.
    /// </summary>
    public sealed class ObjectEyeEnabledConverter : IMultiValueConverter
    {
        /// <summary>Shared converter instance for XAML bindings.</summary>
        public static readonly ObjectEyeEnabledConverter Instance = new();

        /// <inheritdoc />
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 2 && values[0] is LevelObject obj)
            {
                string? locale = obj.GetAttr("locale");
                string? displayLocale = values[1] as string;
                bool offLocale = locale is not null && locale != displayLocale;
                return !offLocale;
            }

            return true;
        }
    }
}
