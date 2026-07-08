using System;
using System.Collections.Generic;
using System.Globalization;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Builds star properties with timed-star disclosure.</summary>
    public static class StarFieldBuilder
    {
        /// <summary>Appends the star's timed toggle and optional duration field.</summary>
        public static void Build(
            IList<AttributeFieldViewModel> fields,
            LevelObject star,
            Action onChanged,
            Action onChanging,
            Action rebuild)
        {
            bool timed = Timeout(star) > 0;

            void Structural()
            {
                onChanged();
                rebuild();
            }

            fields.Add(new AttributeFieldViewModel(
                "timed",
                AttrType.Bool,
                () => timed ? "true" : "false",
                v => star.SetAttr("timeout", v == "true" ? "5" : "-1"),
                Structural,
                onChanging));

            if (timed)
            {
                fields.Add(new AttributeFieldViewModel(star, "timeout", AttrType.Number, null, onChanged, onChanging));
            }
        }

        private static double Timeout(LevelObject star)
        {
            return double.TryParse(star.GetAttr("timeout"), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : 0;
        }
    }
}
