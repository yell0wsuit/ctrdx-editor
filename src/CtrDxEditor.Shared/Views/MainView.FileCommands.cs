using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;
using CtrDxEditor.Rendering;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    // File menu commands: new/open/close/save/save-as/screenshot plus the level-settings dialog. These are
    // the view-bound operations that talk to the storage provider, dialogs, and the toast host.
    public partial class MainView
    {
        private IStorageFile? _currentLevelFile;

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
                if (vm.IsModified && !await UnsavedChangesPrompt.ConfirmDiscardAsync("Dialog.Unsaved.New"))
                {
                    return;
                }
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
            IReadOnlyList<LevelWarning> warnings, string promptKey, string proceedKey)
        {
            string body = Localizer.Get("Dialog.Validation.Body") + "\n\n"
                + string.Join("\n", warnings.Select(w => "- " + Localizer.Format(w))) + "\n\n"
                + Localizer.Get(promptKey);
            ConfirmDialog dialog = new()
            {
                Header = Localizer.Get("Dialog.Validation.Header"),
                Message = body,
                PositiveText = Localizer.Get(proceedKey),
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
                await OpenLevelFileAsync(vm, files[0]);
            }
        }

        // The shared load path behind both the Open dialog and a window drop: read, parse, warn, confirm,
        // then hand the XML to the view model. Reading through the stream rather than a local path keeps
        // this working in the browser build, where IStorageFile exposes no filesystem path.
        private async Task OpenLevelFileAsync(EditorViewModel vm, IStorageFile file)
        {
            LevelDocument document;
            IReadOnlyList<LevelWarning> warnings;
            try
            {
                // The read stays here: it is async I/O that never blocks the thread, and on the
                // browser build the storage stream is JS interop that has to run on this thread.
                string xml;
                await using (Stream stream = await file.OpenReadAsync())
                {
                    using StreamReader reader = new(stream);
                    xml = await reader.ReadToEndAsync();
                }

                // Parse and validation are pure CPU over the text and are the part that actually
                // stalls the UI on a large level, so they go off-thread on desktop. The browser is
                // single-threaded (no WasmEnableThreads), where this simply runs inline.
                (document, warnings) = await Task.Run(() =>
                {
                    LevelDocument parsed = LevelDocument.Parse(xml);
                    return (parsed, LevelValidator.Validate(parsed));
                });
            }
            catch (Exception ex)
            {
                // A file that is unreadable or is not a level (anything droppable now reaches this, and the
                // dialog never guaranteed well-formed XML either) would otherwise fault this async void
                // caller with no observer. Report it and leave the open document untouched.
                Notifications()?.Show(new Notification(
                    Localizer.Get("Notification.Open.Failed"),
                    ex.Message,
                    NotificationType.Error));
                return;
            }

            if (warnings.Count > 0
                && !await ConfirmValidationAsync(warnings, "Dialog.Validation.EditPrompt", "Dialog.Validation.EditProceed"))
            {
                return;
            }
            if (vm.IsModified && !await UnsavedChangesPrompt.ConfirmDiscardAsync("Dialog.Unsaved.Open"))
            {
                return;
            }
            vm.LoadLevel(document);
            _currentLevelFile = file;
        }

        private async void Close_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm || !vm.HasDocument)
            {
                return;
            }

            if (vm.IsModified && !await UnsavedChangesPrompt.ConfirmDiscardAsync("Dialog.Unsaved.Close"))
            {
                return;
            }

            vm.CloseLevel();
            _currentLevelFile = null;
            this.FindControl<LevelCanvas>("Canvas")!.InvalidateVisual();
        }

        private async void Save_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm || !vm.HasDocument)
            {
                return;
            }

            // Guarded here rather than only on the menu item, so neither the Ctrl+S chord nor the header's
            // access key can reach a write this platform cannot perform. Where Save is hidden every save is
            // a download, so the chord lands on Save As instead of doing nothing.
            if (_currentLevelFile is null || !vm.CanSaveInPlace)
            {
                await SaveAsAsync(vm);
                return;
            }

            if (await CanSaveAsync(vm) && vm.ToXml() is { } xml)
            {
                await WriteXmlAsync(_currentLevelFile, xml);
                vm.MarkSaved();
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

            string suggested = _currentLevelFile is { Name: { } name }
                ? Path.ChangeExtension(name, ".png")
                : "level.png";

            // Pick the destination first, before doing any rendering, so the dialog opens instantly and the
            // "Saving…" toast can appear the moment the user confirms - covering the render + encode below.
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

            WindowNotificationManager? toasts = Notifications();

            // Show a sticky "Saving…" toast up front (Expiration.Zero = stays until replaced). The manager's
            // MaxItems is 1, so the terminal "Saved"/"Failed" toast below evicts this one in place.
            toasts?.Show(new Notification(
                Localizer.Get("Notification.Screenshot.Saving"),
                string.Empty,
                NotificationType.Information,
                expiration: TimeSpan.Zero));

            // Yield below the render priority so the toast actually paints before the UI-thread render and
            // the encode hog the thread - otherwise a fast save could finish before "Saving…" ever showed.
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

            try
            {
                // The bitmap must be rendered on the UI thread (Avalonia draws RenderTargetBitmap there);
                // the encode+write is what we push off-thread. Disposed here - it holds a GPU surface.
                using RenderTargetBitmap bitmap = canvas.RenderLevelToBitmap()
                    ?? throw new InvalidOperationException("The level could not be rendered.");

                // PNG encoding of a full-resolution level can take a noticeable amount of time. The bitmap
                // is finished rendering and is not touched elsewhere, so encode it on a background thread to
                // keep the editor responsive. (On the single-threaded browser runtime this still runs inline,
                // but harmlessly.) Bitmap.Save writes synchronously, which the browser's destination stream
                // rejects - it supports only async writes - so encode into memory first, then copy to the
                // destination with async writes. The await resumes on the UI thread for the toast below.
                using MemoryStream buffer = new();
                await Task.Run(() => bitmap.Save(buffer, PngBitmapEncoderOptions.Default));
                buffer.Position = 0;
                await using Stream stream = await file.OpenWriteAsync();
                await buffer.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                // Backgrounding the save means an encode/IO failure would otherwise go unobserved on this
                // async void handler; surface it as a toast (which also clears the sticky "Saving…" one).
                toasts?.Show(new Notification(
                    Localizer.Get("Notification.Screenshot.Failed"),
                    ex.Message,
                    NotificationType.Error));
                return;
            }

            // Confirm the save with a toast showing where it landed (a full local path on desktop, the
            // download name in the browser where paths are not exposed).
            string location = file.TryGetLocalPath() ?? file.Name;
            toasts?.Show(new Notification(
                Localizer.Get("Notification.Screenshot.Title"),
                location,
                NotificationType.Success));
        }

        private async void ReviewChanges_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm
                || vm.SavedBaselineXml is not { } baseline
                || vm.ToXml() is not { } currentXml)
            {
                return;
            }
            // Snapshot only: the editor is inert behind the modal dialog, so the diff never needs updating.
            ReviewChangesViewModel dialogVm = new(baseline, currentXml);
            ReviewChangesDialog dialog = new() { DataContext = dialogVm };
            _ = await dialog.ShowAsync();
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
                vm.MarkSaved();
            }
        }

        private static async Task<bool> CanSaveAsync(EditorViewModel vm)
        {
            if (vm.Document is { } doc)
            {
                IReadOnlyList<LevelWarning> warnings = LevelValidator.Validate(doc);
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
            await using StreamWriter writer = new(stream);
            await writer.WriteAsync(xml);
        }
    }
}
