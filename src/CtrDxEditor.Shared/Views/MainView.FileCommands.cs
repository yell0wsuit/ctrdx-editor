using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using AvaloniaDialogs.Views;

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
                await Task.Run(() => bitmap.Save(buffer));
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
