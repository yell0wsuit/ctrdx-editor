using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

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

        private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
        {
            if (TryRunShortcut(EditorShortcuts.ResolveCommand(e.Key, e.KeyModifiers, CmdModifier, OperatingSystem.IsMacOS())))
            {
                e.Handled = true;
            }
        }

        // Executes a resolved shortcut against the view's handlers, applying availability guards (a document
        // is open, undo/redo exists). Returns true only when the command actually ran, so the key is marked
        // Handled only then and otherwise keeps routing to the focused element.
        private bool TryRunShortcut(EditorShortcut shortcut)
        {
#pragma warning disable IDE0010
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
                case EditorShortcut.ZoomIn when DataContext is EditorViewModel { HasDocument: true }:
                    this.FindControl<LevelCanvas>("Canvas")!.ZoomBy(1.2);
                    return true;
                case EditorShortcut.ZoomOut when DataContext is EditorViewModel { HasDocument: true }:
                    this.FindControl<LevelCanvas>("Canvas")!.ZoomBy(1 / 1.2);
                    return true;
                case EditorShortcut.ZoomFit when DataContext is EditorViewModel { HasDocument: true }:
                    this.FindControl<LevelCanvas>("Canvas")!.FitToView();
                    return true;
                case EditorShortcut.Delete when DataContext is EditorViewModel deleteVm:
                    deleteVm.DeleteSelected();
                    this.FindControl<LevelCanvas>("Canvas")!.InvalidateVisual();
                    return true;
                case EditorShortcut.ToggleAnimationPreview when DataContext is EditorViewModel { HasDocument: true } previewVm:
                    previewVm.ToggleAnimationPreviewAll();
                    return true;
                default:
                    return false;
            }
#pragma warning restore IDE0010
        }

        private static bool IsTextInputFocused(object? source)
        {
            return (source as Visual)?.FindAncestorOfType<TextBox>(includeSelf: true) is not null;
        }
    }
}
