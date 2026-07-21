using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using AvaloniaDialogs.Views;

using CtrDxEditor.Rendering;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    // Keyboard shortcut wiring. Resolution is pure (see EditorShortcuts); this file only routes the two
    // phases into that resolver and executes the resolved command against the view's handlers.
    public partial class MainView
    {
        // The command modifier for menu shortcuts: Cmd on macOS, Ctrl elsewhere.
        private static readonly KeyModifiers CmdModifier =
            OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

        // Delete and Space stay on the bubble path so they yield to text editing and focused-button
        // activation: a focused TextBox marks them Handled first, and this handler (handledEventsToo defaults
        // false) never sees them - which is exactly what we want.
        private void WireLocalShortcuts()
        {
            KeyDown += (_, e) =>
            {
                if (TryRunShortcut(EditorShortcuts.ResolveLocal(e.Key, e.KeyModifiers, IsTextInputFocused(e.Source))))
                {
                    e.Handled = true;
                }
            };
        }

        // Registers the command chords at the TopLevel in the tunnel phase, before any focused child can
        // swallow them, so they fire regardless of focus. The TopLevel differs per attach, so this pairs with
        // UnregisterGlobalShortcuts across the visual-tree lifecycle rather than living in the constructor.
        private void RegisterGlobalShortcuts()
        {
            TopLevel.GetTopLevel(this)?.AddHandler(
                KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        }

        private void UnregisterGlobalShortcuts()
        {
            TopLevel.GetTopLevel(this)?.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
        }

        // A dialog hosted in this same TopLevel is modal over the editor, so the chords stand down while one
        // is up - same reasoning as DialogOwnsDrop, and the handler above deliberately sees keys the dialog
        // has already handled, so nothing else would stop them.
        private bool DialogOwnsKeyboard => this.FindControl<ReactiveDialogHost>("DialogHost")?.IsOpen == true;

        private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
        {
            if (TryRunShortcut(EditorShortcuts.ResolveCommand(
                e.Key, e.KeyModifiers, CmdModifier, OperatingSystem.IsMacOS(), DialogOwnsKeyboard)))
            {
                e.Handled = true;
            }
        }

        // Executes a resolved shortcut against the view's handlers, applying availability guards (a document
        // is open, undo/redo exists). Returns true only when the command actually ran, so the key is marked
        // Handled only then and otherwise keeps routing to the focused element.
        private bool TryRunShortcut(EditorShortcut shortcut)
        {
            switch (shortcut)
            {
                case EditorShortcut.New:
                    New_Click(this, new RoutedEventArgs());
                    return true;
                case EditorShortcut.Open:
                    Open_Click(this, new RoutedEventArgs());
                    return true;
                case EditorShortcut.Save when DataContext is EditorViewModel { HasDocument: true }:
                    Save_Click(this, new RoutedEventArgs());
                    return true;
                case EditorShortcut.SaveAs when DataContext is EditorViewModel { HasDocument: true }:
                    SaveAs_Click(this, new RoutedEventArgs());
                    return true;
                case EditorShortcut.Screenshot when DataContext is EditorViewModel { HasDocument: true }:
                    Screenshot_Click(this, new RoutedEventArgs());
                    return true;
                case EditorShortcut.Close when DataContext is EditorViewModel { HasDocument: true }:
                    Close_Click(this, new RoutedEventArgs());
                    return true;
                case EditorShortcut.Undo when DataContext is EditorViewModel { CanUndo: true } undoVm:
                    undoVm.Undo();
                    return true;
                case EditorShortcut.Redo when DataContext is EditorViewModel { CanRedo: true } redoVm:
                    redoVm.Redo();
                    return true;
                case EditorShortcut.SelectAll when DataContext is EditorViewModel { CanSelectAllObjects: true } selectVm:
                    selectVm.SelectAllObjects();
                    return true;
                case EditorShortcut.Copy when DataContext is EditorViewModel { CanCopySelection: true } copyVm:
                    copyVm.CopySelection();
                    return true;
                case EditorShortcut.Cut when DataContext is EditorViewModel { CanCutSelection: true } cutVm:
                    cutVm.CutSelection();
                    return true;
                case EditorShortcut.Paste when DataContext is EditorViewModel { CanPaste: true } pasteVm:
                    (int pasteX, int pasteY) = _canvas.PasteTargetLevelPoint();
                    pasteVm.PasteAt(pasteX, pasteY);
                    return true;
                case EditorShortcut.ZoomIn when DataContext is EditorViewModel { HasDocument: true }:
                    this.FindControl<LevelCanvas>("Canvas")!.ZoomBy(1.2);
                    return true;
                case EditorShortcut.ZoomOut when DataContext is EditorViewModel { HasDocument: true }:
                    this.FindControl<LevelCanvas>("Canvas")!.ZoomBy(1 / 1.2);
                    return true;
                case EditorShortcut.ZoomFit when DataContext is EditorViewModel { HasDocument: true }:
                    this.FindControl<LevelCanvas>("Canvas")!.FitToView();
                    return true;
                case EditorShortcut.Delete when DataContext is EditorViewModel { HasDocument: true } deleteVm:
                    if (deleteVm.SelectedLayers.Count > 0)
                    {
                        LayerDeleteActive_Click(this, new RoutedEventArgs());
                    }
                    else if (deleteVm.CanDeleteSelection)
                    {
                        deleteVm.DeleteSelected();
                        this.FindControl<LevelCanvas>("Canvas")!.InvalidateVisual();
                    }
                    else
                    {
                        return false;
                    }
                    return true;
                case EditorShortcut.ToggleAnimationPreview when DataContext is EditorViewModel { CanToggleAnimationPreview: true } previewVm:
                    previewVm.ToggleAnimationPreviewAll();
                    return true;
                case EditorShortcut.None:
                case EditorShortcut.Save:
                case EditorShortcut.SaveAs:
                case EditorShortcut.Screenshot:
                case EditorShortcut.Close:
                case EditorShortcut.Undo:
                case EditorShortcut.Redo:
                case EditorShortcut.SelectAll:
                case EditorShortcut.Copy:
                case EditorShortcut.Cut:
                case EditorShortcut.Paste:
                case EditorShortcut.ZoomIn:
                case EditorShortcut.ZoomOut:
                case EditorShortcut.ZoomFit:
                case EditorShortcut.Delete:
                case EditorShortcut.ToggleAnimationPreview:
                default:
                    return false;
            }
        }

        private static bool IsTextInputFocused(object? source)
        {
            return (source as Visual)?.FindAncestorOfType<TextBox>(includeSelf: true) is not null;
        }
    }
}
