using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Builds Om Nom's properties, naming the skins its targetType selects.</summary>
    public static class TargetFieldBuilder
    {
        /// <summary>
        /// Appends the skin picker. It is delegate-backed rather than descriptor-driven so a target with no
        /// targetType attribute still shows "Player's choice" instead of an empty box, and so picking that
        /// option removes the attribute again.
        /// </summary>
        public static void Build(
            IList<AttributeFieldViewModel> fields,
            LevelObject target,
            Action onChanged,
            Action onChanging)
        {
            fields.Add(new AttributeFieldViewModel(
                "targetType",
                SkinOptions(),
                () => TargetObject.Skin(target),
                v => TargetObject.SetSkin(target, v),
                onChanged,
                onChanging));
        }

        /// <summary>
        /// Player's choice followed by every skin the game can resolve, labelled from the localization file
        /// (<c>Attr.targetType.N</c>) and falling back to the raw number when a string is missing.
        /// </summary>
        private static AttributeOptionViewModel[] SkinOptions()
        {
            return
            [
                .. Enumerable.Range(0, TargetObject.SkinCount + 1)
                    .Select(value => value.ToString(CultureInfo.InvariantCulture))
                    .Select(value => new AttributeOptionViewModel(
                        value, Localizer.AttributeOption("targetType", value))),
            ];
        }
    }
}
