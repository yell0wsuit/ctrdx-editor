using System;
using System.Collections.Generic;

using Avalonia.Controls;
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
            }
            _vm = DataContext as ContentSetupViewModel;
            if (_vm is not null)
            {
                _vm.Completed += OnCompleted;
            }
        }

        private void OnCompleted()
        {
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
                    Title = Localizer.Get("Dialog.ContentSetup.PickerTitle"),
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

        private void Quit_Click(object? sender, RoutedEventArgs e) => Close();
    }
}
