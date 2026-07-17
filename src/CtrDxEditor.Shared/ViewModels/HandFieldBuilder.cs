using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>
    /// Builds mechanical hand fields: the segment count, then one collapsible section per live segment.
    /// Sections are driven entirely by <c>segmentsCount</c> — slots past it are the game's dead data and are
    /// left out of the panel but never removed from the XML.
    /// </summary>
    public static class HandFieldBuilder
    {
        private static readonly CompositeFormat SegmentHeaderFormat =
            CompositeFormat.Parse(Localizer.Get("Panel.HandSegment"));

        /// <summary>Adds the segment count field and a section per live segment.</summary>
        /// <param name="fields">The panel's flat field list to append to.</param>
        /// <param name="value">The hand being edited.</param>
        /// <param name="onChanged">Invoked after a field writes a value.</param>
        /// <param name="onChanging">Invoked before a field writes, to capture undo state.</param>
        /// <param name="rebuild">Re-runs field construction after the segment count changes.</param>
        public static void Build(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging,
            Action rebuild)
        {
            AttributeFieldViewModel count = new(
                HandObject.CountAttr,
                AttrType.Whole,
                () => HandObject.SegmentCount(value).ToString(CultureInfo.InvariantCulture),
                v =>
                {
                    int n = int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                        ? parsed
                        : 0;
                    HandObject.SetSegmentCount(value, n);
                },
                () =>
                {
                    onChanged();
                    rebuild();
                },
                onChanging);
            fields.Add(count);

            int segments = HandObject.SegmentCount(value);
            for (int i = 1; i <= segments; i++)
            {
                int index = i;
                string header = string.Format(
                    CultureInfo.CurrentCulture, SegmentHeaderFormat, i);

                fields.Add(new AttributeFieldViewModel(
                    HandObject.AngleAttr(index),
                    AttrType.Number,
                    () => value.GetAttr(HandObject.AngleAttr(index)),
                    v =>
                    {
                        double angle = double.TryParse(
                            v, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                            ? parsed
                            : 0;
                        HandObject.SetAngle(value, index, angle);
                    },
                    onChanged,
                    onChanging,
                    labelName: "angle")
                {
                    GroupHeader = header,
                    GroupIndex = i,
                });
                fields.Add(new AttributeFieldViewModel(
                    value, HandObject.LengthAttr(i), AttrType.Number, null, onChanged, onChanging, labelName: "length")
                {
                    GroupHeader = header,
                    GroupIndex = i,
                });
                fields.Add(new AttributeFieldViewModel(
                    value, HandObject.RotatableAttr(i), AttrType.Bool, null, onChanged, onChanging, labelName: "isRotatable")
                {
                    GroupHeader = header,
                    GroupIndex = i,
                });
            }
        }
    }
}
