using System;
using System.Collections.ObjectModel;
using System.Globalization;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>
    /// Builds the rocket properties panel: launch fields with help on impulse/impulseFactor, a star-style
    /// timed-burn disclosure for the game's `time` attribute (-1 = fires until impact, positive = burns
    /// then exhausts), and the shared spin/path controls (same as spike/bouncer).
    /// </summary>
    public static class RocketFieldBuilder
    {
        /// <summary>Appends the rocket's fields to <paramref name="fields"/>.</summary>
        public static void Build(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging,
            Action rebuild)
        {
            bool timed = Time(value) > 0;

            void Structural()
            {
                onChanged();
                rebuild();
            }

            fields.Add(new AttributeFieldViewModel(value, "angle", AttrType.Number, null, onChanged, onChanging));
            fields.Add(new AttributeFieldViewModel(value, "isRotatable", AttrType.Bool, null, onChanged, onChanging));

            fields.Add(new AttributeFieldViewModel(value, "impulse", AttrType.Number, null, onChanged, onChanging)
            {
                HelpText = Localizer.Get("Attr.impulse.Help"),
            });
            fields.Add(new AttributeFieldViewModel(value, "impulseFactor", AttrType.Number, null, onChanged, onChanging)
            {
                HelpText = Localizer.Get("Attr.impulseFactor.Help"),
            });

            fields.Add(new AttributeFieldViewModel(
                "timed",
                AttrType.Bool,
                () => timed ? "true" : "false",
                v => value.SetAttr("time", v == "true" ? "5" : "-1"),
                Structural,
                onChanging));

            if (timed)
            {
                fields.Add(new AttributeFieldViewModel(value, "time", AttrType.Number, null, onChanged, onChanging));
            }

            SpinFieldBuilder.Build(fields, value, onChanged, onChanging, rebuild);
        }

        private static double Time(LevelObject rocket)
        {
            return double.TryParse(rocket.GetAttr("time"), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : 0;
        }
    }
}
