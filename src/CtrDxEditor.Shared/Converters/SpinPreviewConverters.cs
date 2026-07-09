using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia.Data.Converters;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Converters
{
    /// <summary>Converters for object-list live animation preview controls.</summary>
    public static class SpinPreviewConverters
    {
        /// <summary>True for objects that have active spin or orbit data and can show a per-object preview button.</summary>
        public static readonly IValueConverter Available = new SpinPreviewAvailableConverter();

        /// <summary>True for objects that have active spin or orbit data, re-evaluated when object-row state changes.</summary>
        public static readonly IMultiValueConverter AvailableForVersion = new SpinPreviewAvailableForVersionConverter();

        /// <summary>True when the row object is the active object-scoped preview target.</summary>
        public static readonly IMultiValueConverter IsObjectPreviewing = new ObjectAnimationPreviewingConverter();

        private sealed class SpinPreviewAvailableConverter : IValueConverter
        {
            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return value is LevelObject obj && SpinTable.IsSpinnable(obj.Type) && HasPreviewAnimation(obj);
            }

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class SpinPreviewAvailableForVersionConverter : IMultiValueConverter
        {
            public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                return values.Count >= 1
                    && values[0] is LevelObject obj
                    && SpinTable.IsSpinnable(obj.Type)
                    && HasPreviewAnimation(obj);
            }
        }

        private static bool HasPreviewAnimation(LevelObject obj)
        {
            return ElectroAnimation.IsElectro(obj.Type) || ObjectSpin.IsRotatingInPlace(obj) || ObjectSpin.IsOrbital(obj);
        }

        private sealed class ObjectAnimationPreviewingConverter : IMultiValueConverter
        {
            public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                bool previewing = values.Count == 3
                    && values[1] is AnimationPreviewMode.Focused
                    && values[0] is not null
                    && Equals(values[0], values[2]);
                return string.Equals(parameter as string, "Invert", StringComparison.Ordinal) ? !previewing : previewing;
            }
        }
    }
}
