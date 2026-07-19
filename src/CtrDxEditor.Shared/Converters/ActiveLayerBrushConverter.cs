using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Styling;

namespace CtrDxEditor.Converters
{
    /// <summary>
    /// Resolves the full-width row highlight brush for the active layer's TreeViewItem container,
    /// so the whole row (including the expand chevron) is highlighted rather than just the inner
    /// content area. Inputs: [0] the layer's IsActive flag, [1] the container's ActualThemeVariant
    /// (included so the brush re-resolves when the light/dark theme changes). Returns the accent
    /// brush when active, otherwise an unset value so the theme default (transparent) applies.
    /// </summary>
    public sealed class ActiveLayerBrushConverter : IMultiValueConverter
    {
        /// <summary>Shared converter instance for XAML bindings.</summary>
        public static readonly ActiveLayerBrushConverter Instance = new();

        /// <inheritdoc />
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            bool active = values.Count > 0 && values[0] is true;
            if (!active)
            {
                return AvaloniaProperty.UnsetValue;
            }

            ThemeVariant variant = values.Count > 1 && values[1] is ThemeVariant tv ? tv : ThemeVariant.Default;
            return Application.Current is { } app
                && app.TryGetResource("SystemControlHighlightListAccentLowBrush", variant, out object? brush)
                ? brush
                : AvaloniaProperty.UnsetValue;
        }
    }
}
