using System;
using System.Collections.ObjectModel;
using System.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Builds tutorial icon and tutorial text fields for the properties panel.</summary>
    public static class TutorialFieldBuilder
    {
        private static readonly AttributeOptionViewModel[] IconOptions =
        [
            .. Enumerable.Range(0, TutorialObject.IconCount).Select(quad =>
                new AttributeOptionViewModel(
                    TutorialObject.TagForQuad(quad),
                    Localization.Localizer.AttributeOption("icon", TutorialObject.TagForQuad(quad)))),
        ];

        /// <summary>Appends the fields for a tutorial icon or tutorial text object.</summary>
        /// <param name="fields">The properties-panel field collection to append to.</param>
        /// <param name="value">The tutorial object being edited.</param>
        /// <param name="onChanged">Invoked after a field commits a change.</param>
        /// <param name="onChanging">Invoked before a field commits a change.</param>
        /// <param name="rebuild">Repopulates fields after the icon tag changes.</param>
        public static void Build(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging,
            Action rebuild)
        {
            if (TutorialObject.IsText(value.Type))
            {
                fields.Add(new AttributeFieldViewModel(value, "text", AttrType.Text, null, onChanged, onChanging));
                fields.Add(new AttributeFieldViewModel(value, "width", AttrType.Whole, null, onChanged, onChanging));
                return;
            }

            void IconChanged()
            {
                onChanged();
                rebuild();
            }

            fields.Add(new AttributeFieldViewModel(
                "icon",
                IconOptions,
                () => value.Type,
                selectedTag =>
                {
                    int quad = TutorialObject.QuadForTag(selectedTag ?? string.Empty);
                    if (quad >= 0)
                    {
                        TutorialObject.SetIcon(value, quad);
                    }
                },
                IconChanged,
                onChanging));

            fields.Add(new AttributeFieldViewModel(value, "angle", AttrType.Number, null, onChanged, onChanging));
        }
    }
}
