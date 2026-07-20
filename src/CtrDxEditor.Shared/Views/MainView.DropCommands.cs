using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;

using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    // Drag-and-drop opening of level XML. The drop is accepted across the whole window rather than on the
    // canvas alone: dragging from the file manager means the source window overlaps the editor, and a small
    // target would force the user to rearrange windows first. The overlay is the in-app confirmation that
    // the release will land, since the OS drag cursor alone does not say which app will take the file.
    public partial class MainView
    {
        private TopLevel? _dropHost;

        /// <summary>Whether a set of dropped file names is a level the editor will open.</summary>
        /// <remarks>
        /// Only a single .xml is meaningful. Matching on the name rather than a local path keeps the rule
        /// working in the browser build, where dropped files have no filesystem path to inspect. Public so
        /// the rule can be tested directly - DragEventArgs is impractical to construct in a unit test.
        /// </remarks>
        public static bool AcceptsDroppedNames(IReadOnlyList<string> names)
        {
            return names.Count == 1 && names[0].EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
        }

        private static IStorageFile? DroppedLevelFile(DragEventArgs e)
        {
            IStorageItem[]? items = e.DataTransfer?.TryGetFiles();
            return items is not null
                && AcceptsDroppedNames([.. items.Select(i => i.Name)])
                && items[0] is IStorageFile file
                ? file
                : null;
        }

        private void RegisterLevelDrop()
        {
            if (TopLevel.GetTopLevel(this) is { } top)
            {
                _dropHost = top;
                DragDrop.SetAllowDrop(top, true);
                top.AddHandler(DragDrop.DragOverEvent, Host_DragOver);
                top.AddHandler(DragDrop.DragLeaveEvent, Host_DragLeave);
                top.AddHandler(DragDrop.DropEvent, Host_Drop);
            }
        }

        private void UnregisterLevelDrop()
        {
            if (_dropHost is { } top)
            {
                top.RemoveHandler(DragDrop.DragOverEvent, Host_DragOver);
                top.RemoveHandler(DragDrop.DragLeaveEvent, Host_DragLeave);
                top.RemoveHandler(DragDrop.DropEvent, Host_Drop);
                DragDrop.SetAllowDrop(top, false);
                _dropHost = null;
            }

            SetDropOverlayVisible(false);
        }

        private void SetDropOverlayVisible(bool visible)
        {
            if (this.FindControl<Control>("DropOverlay") is { } overlay)
            {
                overlay.IsVisible = visible;
            }
        }

        private void Host_DragOver(object? sender, DragEventArgs e)
        {
            bool accepted = DroppedLevelFile(e) is not null;
            e.DragEffects = accepted ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
            SetDropOverlayVisible(accepted);
        }

        private void Host_DragLeave(object? sender, DragEventArgs e)
        {
            SetDropOverlayVisible(false);
        }

        // DataTransfer is only valid for the duration of this synchronous handler, so the file reference is
        // taken here and the load is continued afterwards.
        private async void Host_Drop(object? sender, DragEventArgs e)
        {
            e.Handled = true;
            SetDropOverlayVisible(false);

            if (DataContext is EditorViewModel vm && DroppedLevelFile(e) is { } file)
            {
                await OpenLevelFileAsync(vm, file);
            }
        }
    }
}
