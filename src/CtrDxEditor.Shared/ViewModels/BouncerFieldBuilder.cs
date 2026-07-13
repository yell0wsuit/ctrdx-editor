using System;
using System.Collections.ObjectModel;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Builds bouncer fields while keeping its width attribute and XML name synchronized.</summary>
    internal static class BouncerFieldBuilder
    {
        /// <summary>Adds the editable angle and width fields.</summary>
        public static void Build(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging,
            Action rebuild)
        {
            AttributeOptionViewModel[] sizeOptions =
            [
                new("1", Localizer.Get("Attr.sizeSmall")),
                new("2", Localizer.Get("Attr.sizeBig")),
            ];

            fields.Add(new AttributeFieldViewModel(value, "angle", AttrType.Number, null, onChanged, onChanging));
            fields.Add(new AttributeFieldViewModel(
                "size",
                sizeOptions,
                () => BouncerObject.Size(value),
                v => BouncerObject.SetSize(value, v),
                onChanged,
                onChanging));
            SpinFieldBuilder.Build(fields, value, onChanged, onChanging, rebuild);
        }
    }
}
