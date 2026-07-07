using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

using AvaloniaDialogs.Views;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
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
        private IStorageFile? _currentLevelFile;
        private WindowNotificationManager? _notifications;

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
            canvas.BeginDocumentEdit = () => (DataContext as EditorViewModel)?.BeginUndoTransaction();
            canvas.CompleteDocumentEdit = () => (DataContext as EditorViewModel)?.CompleteUndoTransaction();

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
                bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
#pragma warning disable IDE0010
                switch (e.Key)
                {
                    case Key.Z when ctrl && !shift && DataContext is EditorViewModel { CanUndo: true } undoVm:
                        undoVm.Undo();
                        e.Handled = true;
                        break;
                    case Key.Z when ctrl && shift && DataContext is EditorViewModel { CanRedo: true } redoVm:
                        redoVm.Redo();
                        e.Handled = true;
                        break;
                    case Key.Y when ctrl && !OperatingSystem.IsMacOS() && DataContext is EditorViewModel { CanRedo: true } redoVm:
                        redoVm.Redo();
                        e.Handled = true;
                        break;
                    case Key.Delete when DataContext is EditorViewModel vm:
                        vm.DeleteSelected();
                        canvas.InvalidateVisual();
                        e.Handled = true;
                        break;
                    case Key.OemPlus or Key.Add when ctrl:
                        if (DataContext is EditorViewModel { HasDocument: true })
                        {
                            canvas.ZoomBy(1.2);
                            e.Handled = true;
                        }
                        break;
                    case Key.OemMinus or Key.Subtract when ctrl:
                        if (DataContext is EditorViewModel { HasDocument: true })
                        {
                            canvas.ZoomBy(1 / 1.2);
                            e.Handled = true;
                        }
                        break;
                    case Key.D0 or Key.NumPad0 when ctrl:
                        if (DataContext is EditorViewModel { HasDocument: true })
                        {
                            canvas.FitToView();
                            e.Handled = true;
                        }
                        break;
                }
#pragma warning restore IDE0010
            };
        }

        private void Undo_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.Undo();
            }
        }

        private void Redo_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.Redo();
            }
        }

        private void Delete_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.DeleteSelected();
                this.FindControl<LevelCanvas>("Canvas")!.InvalidateVisual();
            }
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
                    if (canvas.AddAtCenter(element)) // a click: drop at the level center
                    {
                        _ = canvas.Focus();
                    }
                }
                else
                {
                    Point onCanvas = e.GetPosition(canvas);
                    if (new Rect(canvas.Bounds.Size).Contains(onCanvas))
                    {
                        if (canvas.DropElement(element, onCanvas)) // dragged onto the canvas
                        {
                            _ = canvas.Focus();
                        }
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

        private async void New_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm)
            {
                return;
            }
            LevelSettingsViewModel dialogVm = LevelSettingsViewModel.ForNew();
            dialogVm.LoadDecoration(vm.CurrentSettingsSnapshot);
            LevelSettingsDialog dialog = new() { DataContext = dialogVm };
            // Fill in the background/candy thumbnails progressively off the UI thread; the dialog opens at once.
            _ = LoadBackgroundThumbnailsAsync(dialogVm, vm.Sprites);
            _ = LoadCandyThumbnailsAsync(dialogVm, vm.Sprites);
            LoadSupportThumbnails(dialogVm, vm.Sprites);
            Optional<LevelSettings> result = await dialog.ShowAsync();
            if (result.GetValueOrDefault() is { } settings)
            {
                _currentLevelFile = null;
                (int ropeSkin, int background, int candySkin, int omNomSupport) = dialogVm.ResolveDecoration(Random.Shared);
                vm.NewLevel(settings, ropeSkin, background, candySkin, omNomSupport);

                dialogVm.WriteDecorationInto(vm.CurrentSettingsSnapshot);
                if (vm.Settings is { } store)
                {
                    await store.SaveAsync(vm.CurrentSettingsSnapshot);
                }
            }
        }

        // Decodes each background's picker thumbnail on a background thread and assigns it back on the
        // UI thread (this runs in the UI SynchronizationContext), so the cards fill in as they load.
        private static async Task LoadBackgroundThumbnailsAsync(LevelSettingsViewModel dialogVm, SpriteCache sprites)
        {
            foreach (BackgroundOption option in dialogVm.BackgroundOptions)
            {
                if (option.Id <= 0)
                {
                    continue;
                }
                int id = option.Id;
                option.Thumbnail = await Task.Run(() => sprites.GetBackgroundThumbnail(id));
            }
        }

        // Warms each candy skin's atlas off the UI thread (the heavy PNG decode), then composites its
        // small preview on the UI thread, so the picker cards fill in progressively. Random has no preview.
        private static async Task LoadCandyThumbnailsAsync(LevelSettingsViewModel dialogVm, SpriteCache sprites)
        {
            foreach (CandySkinOption option in dialogVm.CandySkinOptions)
            {
                if (option.Id < 0)
                {
                    continue;
                }
                int skin = option.Id;
                await Task.Run(() => sprites.PreloadCandySkin(skin));
                option.Thumbnail = sprites.GetThumbnail("candy", skin);
            }
        }

        // Composites each sitting-platform preview (Om Nom on the platform). char_supports is already
        // preloaded, so these are cheap UI-thread composites - no off-thread decode needed. Random has none.
        private static void LoadSupportThumbnails(LevelSettingsViewModel dialogVm, SpriteCache sprites)
        {
            foreach (OmNomSupportOption option in dialogVm.OmNomSupportOptions)
            {
                if (option.Id < 0)
                {
                    continue;
                }
                option.Thumbnail = sprites.GetThumbnail("target", 0, option.Id);
            }
        }

        private async void LevelSettings_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm || vm.CurrentSettings is not { } current)
            {
                return;
            }
            LevelSettingsViewModel dialogVm = LevelSettingsViewModel.ForEdit(
                current, vm.ActiveRopeSkin, vm.ActiveBackground, vm.ActiveCandySkin, vm.ActiveOmNomSupport);
            LevelSettingsDialog dialog = new() { DataContext = dialogVm };
            // Fill in the background/candy/platform thumbnails progressively off the UI thread; the dialog opens at once.
            _ = LoadBackgroundThumbnailsAsync(dialogVm, vm.Sprites);
            _ = LoadCandyThumbnailsAsync(dialogVm, vm.Sprites);
            LoadSupportThumbnails(dialogVm, vm.Sprites);
            Optional<LevelSettings> result = await dialog.ShowAsync();
            if (result.GetValueOrDefault() is { } settings)
            {
                vm.UpdateLevelSettings(settings);
                // Apply the chosen decoration to the live editor (Random resolves to a concrete id);
                // the canvas repaints via LevelCanvas's affectsRender on these properties.
                (int ropeSkin, int background, int candySkin, int omNomSupport) = dialogVm.ResolveDecoration(Random.Shared);
                vm.ActiveRopeSkin = ropeSkin;
                vm.ActiveBackground = background;
                vm.ActiveCandySkin = candySkin;
                vm.ActiveOmNomSupport = omNomSupport;
            }
        }

        // Shows the non-blocking validation warning; returns true when the user chooses to proceed.
        private static async Task<bool> ConfirmValidationAsync(
            IReadOnlyList<string> warnings, string promptKey, string proceedKey)
        {
            string body = Localizer.Get("Dialog.Validation.Body") + "\n\n"
                + string.Join("\n", warnings.Select(w => "- " + w)) + "\n\n"
                + Localizer.Get(promptKey);
            TwofoldDialog dialog = new()
            {
                Width = 460,
                ButtonMargin = new Thickness(4, 12, 4, 0),
                Message = body,
                PositiveText = Localizer.Get(proceedKey),
                NegativeText = Localizer.Get("Dialog.Common.Cancel"),
            };
            Optional<bool> confirmed = await dialog.ShowAsync();
            return confirmed.GetValueOrDefault();
        }

        private static async Task<bool> ConfirmCloseAsync()
        {
            ConfirmDialog dialog = new()
            {
                Header = Localizer.Get("Dialog.Close.Header"),
                Message = Localizer.Get("Dialog.Close.Body"),
                PositiveText = Localizer.Get("Dialog.Close.Proceed"),
                NegativeText = Localizer.Get("Dialog.Common.Cancel"),
            };
            Optional<bool> confirmed = await dialog.ShowAsync();
            return confirmed.GetValueOrDefault();
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
                string xml = await reader.ReadToEndAsync();

                IReadOnlyList<string> warnings = LevelValidator.Validate(LevelDocument.Parse(xml));
                if (warnings.Count > 0
                    && !await ConfirmValidationAsync(warnings, "Dialog.Validation.EditPrompt", "Dialog.Validation.EditProceed"))
                {
                    return;
                }
                vm.LoadLevelXml(xml);
                _currentLevelFile = files[0];
            }
        }

        private async void Close_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm || !vm.HasDocument)
            {
                return;
            }

            if (await ConfirmCloseAsync())
            {
                vm.CloseLevel();
                _currentLevelFile = null;
                this.FindControl<LevelCanvas>("Canvas")!.InvalidateVisual();
            }
        }

        private async void Save_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm || !vm.HasDocument)
            {
                return;
            }

            if (_currentLevelFile is null)
            {
                await SaveAsAsync(vm);
                return;
            }

            if (await CanSaveAsync(vm) && vm.ToXml() is { } xml)
            {
                await WriteXmlAsync(_currentLevelFile, xml);
            }
        }

        private async void SaveAs_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                await SaveAsAsync(vm);
            }
        }

        private async void Screenshot_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm || !vm.HasDocument)
            {
                return;
            }

            LevelCanvas canvas = this.FindControl<LevelCanvas>("Canvas")!;
            // The bitmap must be rendered on the UI thread (Avalonia draws RenderTargetBitmap there); the
            // encode+write below is what we push off-thread. Disposed at method end - it holds a GPU surface.
            using RenderTargetBitmap? bitmap = canvas.RenderLevelToBitmap();
            if (bitmap is null)
            {
                return;
            }

            string suggested = _currentLevelFile is { Name: { } name }
                ? Path.ChangeExtension(name, ".png")
                : "level.png";

            IStorageFile? file = await TopLevel.GetTopLevel(this)!.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = Localizer.Get("Dialog.Screenshot.Title"),
                    DefaultExtension = "png",
                    SuggestedFileName = suggested,
                    FileTypeChoices = [new FilePickerFileType(Localizer.Get("Dialog.FileType.Png")) { Patterns = ["*.png"] }],
                });

            if (file is null)
            {
                return;
            }

            // PNG encoding of a full-resolution level can take a noticeable amount of time. The bitmap is
            // finished rendering and is not touched elsewhere, so encode + write it on a background thread
            // to keep the editor responsive. (On the single-threaded browser runtime this still runs inline,
            // but harmlessly.) The await resumes on the UI thread for the toast below.
            await using (Stream stream = await file.OpenWriteAsync())
            {
                await Task.Run(() => bitmap.Save(stream));
            }

            // Confirm the save with a toast showing where it landed (a full local path on desktop, the
            // download name in the browser where paths are not exposed).
            string location = file.TryGetLocalPath() ?? file.Name;
            Notifications()?.Show(new Notification(
                Localizer.Get("Notification.Screenshot.Title"),
                location,
                NotificationType.Success));
        }

        // The toast host, created lazily against the current TopLevel and reused. Null only before the
        // view is attached to a window, which cannot happen from a menu click.
        private WindowNotificationManager? Notifications()
        {
            if (_notifications is null && TopLevel.GetTopLevel(this) is { } top)
            {
                _notifications = new WindowNotificationManager(top)
                {
                    Position = NotificationPosition.BottomRight,
                    MaxItems = 3,
                };
            }
            return _notifications;
        }

        private async Task SaveAsAsync(EditorViewModel vm)
        {
            if (!vm.HasDocument || !await CanSaveAsync(vm))
            {
                return;
            }

            IStorageFile? file = await TopLevel.GetTopLevel(this)!.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = Localizer.Get("Dialog.Save.Title"),
                    DefaultExtension = "xml",
                    SuggestedFileName = _currentLevelFile?.Name ?? "level.xml",
                    FileTypeChoices = [new FilePickerFileType(Localizer.Get("Dialog.FileType.LevelXml")) { Patterns = ["*.xml"] }],
                });

            if (file is not null && vm.ToXml() is { } xml)
            {
                await WriteXmlAsync(file, xml);
                _currentLevelFile = file;
            }
        }

        private static async Task<bool> CanSaveAsync(EditorViewModel vm)
        {
            if (vm.Document is { } doc)
            {
                IReadOnlyList<string> warnings = LevelValidator.Validate(doc);
                if (warnings.Count > 0
                    && !await ConfirmValidationAsync(warnings, "Dialog.Validation.SavePrompt", "Dialog.Validation.SaveProceed"))
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task WriteXmlAsync(IStorageFile file, string xml)
        {
            await using Stream stream = await file.OpenWriteAsync();
            if (stream.CanSeek)
            {
                stream.SetLength(0);
            }
            await using StreamWriter writer = new(stream);
            await writer.WriteAsync(xml);
        }
    }
}
