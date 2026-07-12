using System;
using System.Collections.ObjectModel;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Builds conveyor-specific fields mapping editor controls to the game's transporter XML.</summary>
    public static class ConveyorFieldBuilder
    {
        private static readonly AttributeOptionViewModel[] DirectionOptions =
        [
            new("forward", Localization.Localizer.AttributeOption("direction", "forward")),
            new("backward", Localization.Localizer.AttributeOption("direction", "backward")),
        ];

        /// <summary>
        /// Adds the Automatic checkbox (backed by the <c>type</c> attribute: checked removes it, unchecked
        /// writes "manual"), the direction enum, and the numeric velocity/length/width/angle fields.
        /// velocity and direction stay visible in manual mode; the game ignores them there.
        /// </summary>
        /// <param name="fields">The properties-panel field collection to append to.</param>
        /// <param name="value">The conveyor object being edited.</param>
        /// <param name="onChanged">Invoked after a field commits a change.</param>
        /// <param name="onChanging">Invoked before a field commits a change (for undo capture).</param>
        /// <param name="rebuild">Repopulates the panel when a field toggles which fields are shown.</param>
        public static void Build(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging,
            Action rebuild)
        {
            fields.Add(new AttributeFieldViewModel(
                "auto",
                AttrType.Bool,
                () => ConveyorObject.IsAuto(value) ? "true" : "false",
                v => ConveyorObject.SetAuto(value, v == "true"),
                onChanged,
                onChanging));

            fields.Add(new AttributeFieldViewModel(
                "direction",
                DirectionOptions,
                () => value.GetAttr("direction") ?? "forward",
                v => value.SetAttr("direction", v ?? "forward"),
                onChanged,
                onChanging));

            fields.Add(new AttributeFieldViewModel(value, "velocity", AttrType.Number, null, onChanged, onChanging));
            fields.Add(new AttributeFieldViewModel(value, "length", AttrType.Number, null, onChanged, onChanging));
            fields.Add(new AttributeFieldViewModel(value, "width", AttrType.Number, null, onChanged, onChanging));
            fields.Add(new AttributeFieldViewModel(value, "angle", AttrType.Number, null, onChanged, onChanging));

            _ = rebuild;
        }
    }
}
