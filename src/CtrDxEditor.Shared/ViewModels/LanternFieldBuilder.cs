using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;

namespace CtrDxEditor.ViewModels
{
    /// <summary>
    /// Builds the lantern's <c>candyCaptured</c> toggle plus the shared mover-path movement controls
    /// (None / Orbit / Polyline), the same editor grabs use. The lantern is not a self-spinner, so no
    /// spin controls are added.
    /// </summary>
    public static class LanternFieldBuilder
    {
        /// <summary>Appends the lantern's fields, in panel order, to <paramref name="fields"/>.</summary>
        public static void Build(
            IList<AttributeFieldViewModel> fields,
            LevelObject lantern,
            Action onChanged,
            Action onChanging,
            Action rebuild)
        {
            fields.Add(new AttributeFieldViewModel(lantern, "candyCaptured", AttrType.Bool, null, onChanged, onChanging));
            SpinFieldBuilder.BuildMovement(fields, lantern, onChanged, onChanging, rebuild);
        }
    }
}
