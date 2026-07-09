using System.Collections.ObjectModel;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Builds spike-specific fields that map editor controls to the game's spike XML shape.</summary>
    internal static class SpikeFieldBuilder
    {
        private static readonly AttributeOptionViewModel[] SizeOptions =
        [
            new("1", "1"),
            new("2", "2"),
            new("3", "3"),
            new("4", "4"),
        ];

        private static readonly AttributeOptionViewModel[] GroupOptions =
        [
            new("0", "0"),
            new("1", "1"),
            new("2", "2"),
        ];

        /// <summary>Adds spike angle, size, toggle, and conditional group fields.</summary>
        public static void Build(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            System.Action onChanged,
            System.Action onChanging,
            System.Action rebuild)
        {
            fields.Add(new AttributeFieldViewModel(value, "angle", AttrType.Number, null, onChanged, onChanging));
            fields.Add(new AttributeFieldViewModel(
                "size",
                SizeOptions,
                () => SpikeObject.Size(value),
                v => SpikeObject.SetSize(value, v),
                onChanged,
                onChanging));
            fields.Add(new AttributeFieldViewModel(
                "toggled",
                AttrType.Bool,
                () => SpikeObject.IsToggled(value) ? "true" : "false",
                v =>
                {
                    SpikeObject.SetToggled(value, v == "true");
                    rebuild();
                },
                onChanged,
                onChanging));

            if (SpikeObject.IsToggled(value))
            {
                fields.Add(new AttributeFieldViewModel(
                    "toggleGroup",
                    GroupOptions,
                    () => SpikeObject.Group(value),
                    v => SpikeObject.SetGroup(value, v),
                    onChanged,
                    onChanging));
            }

            SpinFieldBuilder.Build(fields, value, onChanged, onChanging, rebuild);
        }
    }
}
