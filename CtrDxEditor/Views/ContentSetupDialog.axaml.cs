using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using AvaloniaDialogs.Views;

using CtrDxEditor.Content;
using CtrDxEditor.Localization;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>First-run content setup, shown as a dialog over the main window. Returns the resolved content path (empty when the user quits).</summary>
    public partial class ContentSetupDialog : BaseDialog<string>
    {
        private ContentSetupViewModel? _vm;
        private TwofoldDialog? _cancelConfirm;

        public ContentSetupDialog()
        {
            AvaloniaXamlLoader.Load(this);
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            _vm?.Completed -= OnCompleted;
            _vm = DataContext as ContentSetupViewModel;
            _vm?.Completed += OnCompleted;
        }

        private void OnCompleted()
        {
            // A download that finishes while the cancel confirmation is still open would
            // otherwise leave that nested dialog orphaned when the setup dialog closes.
            // Dismiss it first (child before parent), then close the setup dialog.
            _cancelConfirm?.Close();

            if (_vm?.ResolvedContentPath is string path)
            {
                Close(path);
            }
        }

        private async void Locate_Click(object? sender, RoutedEventArgs e)
        {
            if (_vm is null || TopLevel.GetTopLevel(this) is not TopLevel top)
            {
                return;
            }

            IReadOnlyList<IStorageFolder> folders = await top.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = Localizer.Get("Dialog.Common.PickerTitle"),
                    AllowMultiple = false,
                });
            if (folders.Count > 0 && folders[0].TryGetLocalPath() is string path)
            {
                _vm.ApplyLocatedFolder(path);
            }
        }

        private async void DownloadManually_Click(object? sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is TopLevel top)
            {
                _ = await top.Launcher.LaunchUriAsync(new Uri(ContentDownloader.ReleasesPageUrl));
            }
        }

        private async void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            if (_vm is null)
            {
                return;
            }

            // Confirm before aborting; the download keeps running behind this nested dialog.
            // Tracked so OnCompleted can dismiss it if the download finishes first.
            _cancelConfirm = new TwofoldDialog
            {
                // TwofoldDialog defaults to a narrow 300px width with two equal columns, which
                // clips longer button labels; widen it and tighten the button margins so they fit.
                Width = 420,
                ButtonMargin = new Thickness(4, 12, 4, 0),
                Message = Localizer.Get("Dialog.ContentSetup.CancelConfirm"),
                PositiveText = Localizer.Get("Dialog.ContentSetup.CancelConfirm.Yes"),
                NegativeText = Localizer.Get("Dialog.ContentSetup.CancelConfirm.No"),
            };
            try
            {
                Optional<bool> confirmed = await _cancelConfirm.ShowAsync();
                if (confirmed.GetValueOrDefault())
                {
                    _vm.CancelDownload();
                }
            }
            finally
            {
                _cancelConfirm = null;
            }
        }

        private void Quit_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
