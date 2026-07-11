using System.Collections.ObjectModel;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Builds bouncer fields while keeping its width attribute and XML name synchronized.</summary>
    internal static class BouncerFieldBuilder
    {
        private static readonly AttributeOptionViewModel[] SizeOptions =
        [
            new("1", "1"),
            new("2", "2"),
        ];

        /// <summary>Adds the editable angle and width fields.</summary>
        public static void Build(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            System.Action onChanged,
            System.Action onChanging)
        {
            fields.Add(new AttributeFieldViewModel(value, "angle", AttrType.Number, null, onChanged, onChanging));
            fields.Add(new AttributeFieldViewModel(
                "size",
                SizeOptions,
                () => BouncerObject.Size(value),
                v => BouncerObject.SetSize(value, v),
                onChanged,
                onChanging));
        }
    }
}
