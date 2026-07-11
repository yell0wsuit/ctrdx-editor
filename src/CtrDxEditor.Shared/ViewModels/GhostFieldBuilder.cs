using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;

namespace CtrDxEditor.ViewModels
{
    /// <summary>
    /// Builds the ghost morph toggles and conditionally exposes the grab radius and bouncer angle.
    /// Structural toggle changes rebuild the panel so its conditional fields re-evaluate.
    /// </summary>
    public static class GhostFieldBuilder
    {
        /// <summary>Appends the ghost's fields in panel order.</summary>
        /// <param name="fields">The property-panel field collection.</param>
        /// <param name="ghost">The ghost object being edited.</param>
        /// <param name="onChanged">Called after an attribute changes.</param>
        /// <param name="onChanging">Called before an attribute changes.</param>
        /// <param name="rebuild">Called after a morph toggle changes.</param>
        public static void Build(
            IList<AttributeFieldViewModel> fields,
            LevelObject ghost,
            Action onChanged,
            Action onChanging,
            Action rebuild)
        {
            void Structural()
            {
                onChanged();
                rebuild();
            }

            // The grab toggle is the radius driver: turning it on installs the default catch radius
            // (50) so the ring is visible; turning it off writes the -1 auto-rope sentinel. grab is a
            // real stored attribute (the game reads it independently), so we set both grab and radius.
            fields.Add(new AttributeFieldViewModel(
                "grab",
                AttrType.Bool,
                () => Bool(ghost, "grab") ? "true" : "false",
                v =>
                {
                    bool on = v == "true";
                    ghost.SetAttr("grab", on ? "true" : "false");
                    ghost.SetAttr("radius", on ? "50" : "-1");
                },
                Structural,
                onChanging));
            fields.Add(new AttributeFieldViewModel(ghost, "bubble", AttrType.Bool, null, Structural, onChanging));
            fields.Add(new AttributeFieldViewModel(ghost, "bouncer", AttrType.Bool, null, Structural, onChanging));

            if (Bool(ghost, "grab"))
            {
                fields.Add(new AttributeFieldViewModel(ghost, "radius", AttrType.Whole, null, onChanged, onChanging));
            }
            if (Bool(ghost, "bouncer"))
            {
                fields.Add(new AttributeFieldViewModel(ghost, "angle", AttrType.Number, null, onChanged, onChanging));
            }
        }

        private static bool Bool(LevelObject ghost, string name)
        {
            return bool.TryParse(ghost.GetAttr(name), out bool b) && b;
        }
    }
}
