using System;
using System.Collections.ObjectModel;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Builds ant-conveyor fields, including semantic closure backed by the raw relative path.</summary>
    public static class AntFieldBuilder
    {
        /// <summary>Adds move speed, semantic closure, and raw path fields in editor-friendly order.</summary>
        /// <param name="fields">Property field collection to append to.</param>
        /// <param name="ants">Selected ant-conveyor object.</param>
        /// <param name="onChanged">Invoked after a field commits a change.</param>
        /// <param name="onChanging">Invoked before a field commits a change for undo capture.</param>
        public static void Build(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject ants,
            Action onChanged,
            Action onChanging)
        {
            fields.Add(new AttributeFieldViewModel(
                ants,
                "moveSpeed",
                AttrType.Number,
                null,
                onChanged,
                onChanging));

            fields.Add(new AttributeFieldViewModel(
                "closedLoop",
                AttrType.Bool,
                () => AntPath.IsClosed(ants.GetAttr("path")) ? "true" : "false",
                value => AntPath.SetClosed(ants, value == "true"),
                onChanged,
                onChanging));

            fields.Add(new AttributeFieldViewModel(
                ants,
                "path",
                AttrType.Text,
                null,
                onChanged,
                onChanging));
        }
    }
}
