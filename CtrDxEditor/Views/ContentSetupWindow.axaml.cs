using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    public partial class ContentSetupWindow : Window
    {
        private readonly ContentSetupViewModel _vm;

        // Parameterless ctor for the XAML designer only.
        public ContentSetupWindow() : this(
            new ContentSetupViewModel(string.Empty, (_, _, _) => Task.CompletedTask, _ => { }))
        {
        }

        public ContentSetupWindow(ContentSetupViewModel vm)
        {
            _vm = vm;
            AvaloniaXamlLoader.Load(this);
            DataContext = vm;
        }

        private async void Locate_Click(object? sender, RoutedEventArgs e)
        {
            IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = "Select the content folder", AllowMultiple = false });
            if (folders.Count > 0 && folders[0].TryGetLocalPath() is string path)
            {
                _vm.ApplyLocatedFolder(path);
            }
        }

        private async void OpenReleases_Click(object? sender, RoutedEventArgs e)
        {
            _ = await Launcher.LaunchUriAsync(new Uri(ContentDownloader.ReleasesPageUrl));
        }

        private void Quit_Click(object? sender, RoutedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
            else
            {
                Close();
            }
        }
    }
}
