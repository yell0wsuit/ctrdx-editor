using System;
using System.Collections.Generic;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

using CtrDxEditor.Localization;
using CtrDxEditor.Rendering;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>Main editor view containing menus, palette, property panel, object list, and canvas.</summary>
    public partial class MainView : UserControl
    {
        private EditorViewModel? _mutatedSubscription;
        private readonly Action _invalidateCanvas;

        /// <summary>Creates the main editor view and wires input gestures.</summary>
        public MainView()
        {
            AvaloniaXamlLoader.Load(this);
            LevelCanvas canvas = this.FindControl<LevelCanvas>("Canvas")!;
            _invalidateCanvas = canvas.InvalidateVisual;
            DataContextChanged += (_, _) => WireObjectMutated();
            WireObjectMutated();

            canvas.PlaceAt = (element, x, y) =>
                DataContext is EditorViewModel vm ? vm.PlaceObject(element, x, y) : null;
            canvas.ToggleLock = obj => (DataContext as EditorViewModel)?.ToggleLock(obj);
            canvas.SelectedObjectMoved = () => (DataContext as EditorViewModel)?.RefreshFieldValues();

            // Palette placement is an internal pointer-capture drag (no OS drag-drop, so no OS drag image):
            // a click drops at center, a drag drops where it lands on the canvas, a drag off-canvas cancels.
            // Buttons mark left PointerPressed as Handled for their own click logic, so the handlers are
            // registered with handledEventsToo to still see it.
            ItemsControl paletteList = this.FindControl<ItemsControl>("PaletteList")!;
            paletteList.AddHandler(
                PointerPressedEvent, PaletteItem_PointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
            paletteList.AddHandler(
                PointerMovedEvent, PaletteItem_PointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
            paletteList.AddHandler(
                PointerReleasedEvent, PaletteItem_PointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            // Menu hint text (⌘/Ctrl) is bound in XAML via ShortcutHint; here we only handle the keys.
            // MenuItem.InputGesture only renders text and wouldn't trigger Click-driven items anyway.
            KeyModifiers cmdModifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
            KeyDown += (_, e) =>
            {
                bool ctrl = e.KeyModifiers.HasFlag(cmdModifier);
#pragma warning disable IDE0010
                switch (e.Key)
                {
                    case Key.Delete when DataContext is EditorViewModel vm:
                        vm.DeleteSelected();
                        canvas.InvalidateVisual();
                        e.Handled = true;
                        break;
                    case Key.OemPlus or Key.Add when ctrl:
                        canvas.ZoomBy(1.2);
                        e.Handled = true;
                        break;
                    case Key.OemMinus or Key.Subtract when ctrl:
                        canvas.ZoomBy(1 / 1.2);
                        e.Handled = true;
                        break;
                    case Key.D0 or Key.NumPad0 when ctrl:
                        canvas.FitToView();
                        e.Handled = true;
                        break;
                }
#pragma warning restore IDE0010
            };
        }

        private void SnapToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.SnapEnabled = !vm.SnapEnabled;
            }
        }

        private void ShowHitboxesToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.ShowHitboxes = !vm.ShowHitboxes;
            }
        }

        private void ShowMobileHitboxesToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.ShowMobileHitboxes = !vm.ShowMobileHitboxes;
            }
        }

        private void ObjectList_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.ToggleLock(vm.SelectedObject);
            }
        }

        private void ZoomIn_Click(object? sender, RoutedEventArgs e)
        {
            this.FindControl<LevelCanvas>("Canvas")!.ZoomBy(1.2);
        }

        private void ZoomOut_Click(object? sender, RoutedEventArgs e)
        {
            this.FindControl<LevelCanvas>("Canvas")!.ZoomBy(1 / 1.2);
        }

        private void ZoomFit_Click(object? sender, RoutedEventArgs e)
        {
            this.FindControl<LevelCanvas>("Canvas")!.FitToView();
        }

        private const double PaletteDragThreshold = 4;
        private string? _palettePendingElement;
        private Point _palettePressPos;
        private bool _paletteDragging;

        private void PaletteItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Arm a placement on left-press and capture the pointer; the gesture resolves on release.
            _palettePendingElement = null;
            _paletteDragging = false;
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            Button? button = (e.Source as Visual)?.FindAncestorOfType<Button>(includeSelf: true);
            if (button is { Tag: string element, IsEnabled: true })
            {
                _palettePendingElement = element;
                _palettePressPos = e.GetPosition(this);
                e.Pointer.Capture(sender as IInputElement);
            }
        }

        private void PaletteItem_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_palettePendingElement is not string element)
            {
                return;
            }

            if (!_paletteDragging)
            {
                Point now = e.GetPosition(this);
                if (Math.Abs(now.X - _palettePressPos.X) < PaletteDragThreshold
                    && Math.Abs(now.Y - _palettePressPos.Y) < PaletteDragThreshold)
                {
                    return;
                }
                _paletteDragging = true;
            }

            // Show the ghost only while over the canvas; hide it when the cursor leaves.
            LevelCanvas canvas = this.FindControl<LevelCanvas>("Canvas")!;
            Point onCanvas = e.GetPosition(canvas);
            if (new Rect(canvas.Bounds.Size).Contains(onCanvas))
            {
                canvas.ShowGhost(element, onCanvas);
            }
            else
            {
                canvas.HideGhost();
            }
        }

        private void PaletteItem_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_palettePendingElement is string element)
            {
                LevelCanvas canvas = this.FindControl<LevelCanvas>("Canvas")!;
                canvas.HideGhost();
                if (!_paletteDragging)
                {
                    canvas.AddAtCenter(element); // a click: drop at the level center
                }
                else
                {
                    Point onCanvas = e.GetPosition(canvas);
                    if (new Rect(canvas.Bounds.Size).Contains(onCanvas))
                    {
                        canvas.DropElement(element, onCanvas); // dragged onto the canvas
                    }
                    // dragged but released off-canvas: cancel
                }
            }

            e.Pointer.Capture(null);
            _palettePendingElement = null;
            _paletteDragging = false;
        }

        private void WireObjectMutated()
        {
            if (ReferenceEquals(_mutatedSubscription, DataContext))
            {
                return;
            }

            _mutatedSubscription?.ObjectMutated -= _invalidateCanvas;
            _mutatedSubscription?.LevelLoaded -= FocusCanvasAfterLevelLoaded;

            _mutatedSubscription = DataContext as EditorViewModel;
            if (_mutatedSubscription is not null)
            {
                _mutatedSubscription.ObjectMutated += _invalidateCanvas;
                _mutatedSubscription.LevelLoaded += FocusCanvasAfterLevelLoaded;
            }
        }

        private void FocusCanvasAfterLevelLoaded()
        {
            LevelCanvas canvas = this.FindControl<LevelCanvas>("Canvas")!;
            canvas.FitToView();
            _ = canvas.Focus();
        }

        private async void Open_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm)
            {
                return;
            }

            IReadOnlyList<IStorageFile> files = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = Localizer.Get("Dialog.Open.Title"),
                    AllowMultiple = false,
                    FileTypeFilter = [new FilePickerFileType(Localizer.Get("Dialog.FileType.LevelXml")) { Patterns = ["*.xml"] }],
                });

            if (files.Count == 1)
            {
                await using Stream stream = await files[0].OpenReadAsync();
                using StreamReader reader = new(stream);
                vm.LoadLevelXml(await reader.ReadToEndAsync());
            }
        }

        private async void SaveAs_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm)
            {
                return;
            }

            IStorageFile? file = await TopLevel.GetTopLevel(this)!.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = Localizer.Get("Dialog.Save.Title"),
                    DefaultExtension = "xml",
                    SuggestedFileName = "level.xml",
                    FileTypeChoices = [new FilePickerFileType(Localizer.Get("Dialog.FileType.LevelXml")) { Patterns = ["*.xml"] }],
                });

            if (file is not null && vm.ToXml() is { } xml)
            {
                await using Stream stream = await file.OpenWriteAsync();
                await using StreamWriter writer = new(stream);
                await writer.WriteAsync(xml);
            }
        }
    }
}
