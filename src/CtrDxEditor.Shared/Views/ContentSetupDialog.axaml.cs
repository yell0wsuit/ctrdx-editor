using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using AvaloniaDialogs.Views;

using CtrDxEditor.Localization;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>First-run content setup, shown as a dialog over the main window. Returns the resolved content path (empty when the user quits).</summary>
    public partial class ContentSetupDialog : BaseDialog<string>
    {
        private ContentSetupViewModel? _vm;
        private ConfirmDialog? _cancelConfirm;

        /// <summary>Creates the content setup dialog and wires view-model completion handling.</summary>
        public ContentSetupDialog()
        {
            AvaloniaXamlLoader.Load(this);
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_vm is not null)
            {
                _vm.Completed -= OnCompleted;
                _vm.PropertyChanged -= OnViewModelPropertyChanged;
            }
            _vm = DataContext as ContentSetupViewModel;
            if (_vm is not null)
            {
                _vm.Completed += OnCompleted;
                _vm.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Surface errors as a nested dialog on top of the setup dialog rather than inline, so a
            // multi-line invalid-files list never stretches or clips the setup dialog's own layout.
            if (e.PropertyName == nameof(ContentSetupViewModel.ErrorMessage) && _vm?.ErrorMessage is { } message)
            {
                MessageDialog error = new()
                {
                    Header = Localizer.Get("Dialog.Common.Error"),
                    Message = message,
                    IsDanger = true,
                };
                _ = await error.ShowAsync();
            }
        }

        private void OnCompleted()
        {
            // A download that finishes while the cancel confirmation is still open would
            // otherwise leave that nested dialog orphaned when the setup dialog closes.
            // Dismiss it first (child before parent), then close the setup dialog.
            _cancelConfirm?.Close();

            Close("");
        }

        /// <inheritdoc />
        public override bool OnClosing()
        {
            // Browser setup is mandatory because the editor cannot run without content. In
            // particular, keep Escape from dismissing the dialog while an install is in flight.
            return _vm?.CanDismiss ?? true;
        }

        private async void PickZip_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not ContentSetupViewModel vm)
            {
                return;
            }
            IReadOnlyList<IStorageFile> files = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = Localizer.Get("Dialog.ContentSetup.PickZip"),
                    AllowMultiple = false,
                    FileTypeFilter = [new FilePickerFileType("Zip") { Patterns = ["*.zip"] }],
                });
            if (files.Count == 1)
            {
                await using Stream stream = await files[0].OpenReadAsync();
                await vm.InstallFromZipAsync(stream);
            }
        }

        private async void DownloadManually_Click(object? sender, RoutedEventArgs e)
        {
            if (_vm is { } vm && TopLevel.GetTopLevel(this) is TopLevel top)
            {
                _ = await top.Launcher.LaunchUriAsync(new Uri(vm.ManualDownloadUrl));
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
            _cancelConfirm = new ConfirmDialog
            {
                Header = Localizer.Get("Dialog.ContentSetup.CancelConfirm"),
                Message = Localizer.Get("Dialog.ContentSetup.CancelConfirm.Body"),
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

        /// <summary>Dismisses an offered re-download, leaving the installed content as it is.</summary>
        /// <param name="sender">Later button that raised the event.</param>
        /// <param name="e">Click event data.</param>
        /// <remarks>
        /// Distinct from <see cref="Quit_Click"/>: the editor behind this dialog is already running on
        /// content that works, so declining an optional download must not be phrased as quitting.
        /// </remarks>
        private void Later_Click(object? sender, RoutedEventArgs e)
        {
            Close("");
        }
    }
}
