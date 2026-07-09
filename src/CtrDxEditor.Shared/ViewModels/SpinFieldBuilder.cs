using System.Collections.Generic;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Builds rotateSpeed-backed spin property fields for spinnable objects.</summary>
    internal static class SpinFieldBuilder
    {
        /// <summary>Adds spin opt-in, positive speed, and direction fields when supported by <see cref="SpinTable"/>.</summary>
        /// <param name="fields">Property field collection to append to.</param>
        /// <param name="value">Selected object whose spin attributes are edited.</param>
        /// <param name="onChanged">Callback invoked after a field writes to the document.</param>
        /// <param name="onChanging">Callback invoked before a field writes to the document.</param>
        /// <param name="rebuild">Callback that rebuilds fields after spin disclosure changes.</param>
        public static void Build(
            IList<AttributeFieldViewModel> fields,
            LevelObject value,
            System.Action onChanged,
            System.Action onChanging,
            System.Action rebuild)
        {
            if (!SpinTable.IsSpinnable(value.Type))
            {
                return;
            }

            fields.Add(new AttributeFieldViewModel(
                "spin",
                AttrType.Bool,
                () => ObjectSpin.IsSpinning(value) ? "true" : "false",
                v =>
                {
                    bool enabled = v == "true";
                    int speed = ObjectSpin.Speed(value);
                    if (enabled && speed == 0)
                    {
                        speed = ObjectSpin.DefaultSpeed;
                    }

                    ObjectSpin.SetSpin(value, enabled, speed, ObjectSpin.Clockwise(value));
                    rebuild();
                },
                onChanged,
                onChanging));

            if (!ObjectSpin.IsSpinning(value))
            {
                return;
            }

            fields.Add(new AttributeFieldViewModel(
                "spinSpeed",
                AttrType.Whole,
                () => ObjectSpin.Speed(value).ToString(System.Globalization.CultureInfo.InvariantCulture),
                v =>
                {
                    int speed = int.TryParse(v, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int parsed)
                        ? parsed
                        : 0;
                    ObjectSpin.SetSpin(value, enabled: true, speed, ObjectSpin.Clockwise(value));
                    if (speed <= 0)
                    {
                        rebuild();
                    }
                },
                onChanged,
                onChanging));

            fields.Add(new AttributeFieldViewModel(
                "spinClockwise",
                AttrType.Bool,
                () => ObjectSpin.Clockwise(value) ? "true" : "false",
                v => ObjectSpin.SetSpin(value, enabled: true, ObjectSpin.Speed(value), clockwise: v == "true"),
                onChanged,
                onChanging));
        }
    }
}
