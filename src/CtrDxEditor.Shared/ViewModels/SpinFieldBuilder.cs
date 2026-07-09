using System;
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
        /// <param name="canEnable">Whether the spin checkbox should be editable.</param>
        /// <param name="beforeEnable">Optional normalization to run before spin is enabled.</param>
        public static void Build(
            IList<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging,
            Action rebuild,
            bool canEnable = true,
            Action? beforeEnable = null)
        {
            if (!SpinTable.IsSpinnable(value.Type))
            {
                return;
            }

            AttributeFieldViewModel spin = new(
                "spin",
                AttrType.Bool,
                () => ObjectSpin.IsSpinning(value) ? "true" : "false",
                v =>
                {
                    bool enabled = v == "true";
                    if (enabled && !canEnable)
                    {
                        return;
                    }

                    if (enabled)
                    {
                        beforeEnable?.Invoke();
                    }

                    int spinSpeed = ObjectSpin.SpinSpeed(value);
                    if (enabled && spinSpeed == 0)
                    {
                        spinSpeed = ObjectSpin.DefaultSpeed;
                    }

                    ObjectSpin.SetSpin(value, enabled, spinSpeed, ObjectSpin.SpinClockwise(value));
                    rebuild();
                },
                onChanged,
                onChanging)
            {
                IsEnabled = canEnable,
            };
            fields.Add(spin);

            if (ObjectSpin.IsSpinning(value))
            {
                fields.Add(new AttributeFieldViewModel(
                    "spinSpeed",
                    AttrType.Whole,
                    () => ObjectSpin.SpinSpeed(value).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    v =>
                    {
                        int speed = int.TryParse(v, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int parsed)
                            ? parsed
                            : 0;
                        ObjectSpin.SetSpin(value, enabled: true, speed, ObjectSpin.SpinClockwise(value));
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
                    () => ObjectSpin.SpinClockwise(value) ? "true" : "false",
                    v => ObjectSpin.SetSpin(value, enabled: true, ObjectSpin.SpinSpeed(value), clockwise: v == "true"),
                    onChanged,
                    onChanging));
            }

            fields.Add(new AttributeFieldViewModel(
                "spinOrbital",
                AttrType.Bool,
                () => ObjectSpin.IsOrbital(value) ? "true" : "false",
                v =>
                {
                    if (v == "true")
                    {
                        ObjectSpin.SetOrbital(value, enabled: true, ObjectSpin.OrbitRadius(value), ObjectSpin.OrbitClockwise(value));
                    }
                    else
                    {
                        ObjectSpin.SetOrbital(value, enabled: false, ObjectSpin.OrbitRadius(value), ObjectSpin.OrbitClockwise(value));
                    }
                    rebuild();
                },
                onChanged,
                onChanging));

            if (ObjectSpin.IsOrbital(value))
            {
                fields.Add(new AttributeFieldViewModel(
                    "orbitRadius",
                    AttrType.Whole,
                    () => ObjectSpin.OrbitRadius(value).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    v =>
                    {
                        int radius = int.TryParse(v, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int parsed)
                            ? parsed
                            : 0;
                        ObjectSpin.SetOrbital(value, enabled: true, radius, ObjectSpin.OrbitClockwise(value));
                        if (radius <= 0)
                        {
                            rebuild();
                        }
                    },
                    onChanged,
                    onChanging));

                fields.Add(new AttributeFieldViewModel(
                    "orbitClockwise",
                    AttrType.Bool,
                    () => ObjectSpin.OrbitClockwise(value) ? "true" : "false",
                    v => ObjectSpin.SetOrbital(value, enabled: true, ObjectSpin.OrbitRadius(value), clockwise: v == "true"),
                    onChanged,
                    onChanging));
            }
        }
    }
}
