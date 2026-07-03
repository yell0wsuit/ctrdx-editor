using System;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CtrDxEditor.Content;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Backs the first-run Content Setup dialog: download the asset bundle or locate an existing folder.</summary>
    public sealed partial class ContentSetupViewModel : ViewModelBase
    {
        private readonly IContentInstaller _installer;
        private readonly string _downloadContentDir;
        private readonly Func<string, Task> _onInstalled;
        private CancellationTokenSource? _downloadCts;

        [ObservableProperty] public partial bool IsBusy { get; set; }
        [ObservableProperty] public partial double Progress { get; set; }
        [ObservableProperty] public partial string? ErrorMessage { get; set; }

        /// <summary>Raised once content setup succeeds, so the view can close.</summary>
        public event Action? Completed;

        /// <summary>Command that downloads and installs the default asset bundle.</summary>
        public IAsyncRelayCommand DownloadCommand { get; }

        /// <summary>Creates a content setup view model.</summary>
        public ContentSetupViewModel(
            IContentInstaller installer,
            string downloadContentDir,
            Func<string, Task> onInstalled)
        {
            _installer = installer;
            _downloadContentDir = downloadContentDir;
            _onInstalled = onInstalled;
            // AsyncRelayCommand disallows concurrent executions by default, so re-clicks are ignored while busy.
            DownloadCommand = new AsyncRelayCommand(DownloadAsync);
        }

        private async Task DownloadAsync()
        {
            using CancellationTokenSource cts = new();
            _downloadCts = cts;
            IsBusy = true;
            ErrorMessage = null;
            Progress = 0;
            try
            {
                Progress<double> progress = new(p => Progress = p);
                await _installer.InstallFromDownloadAsync(progress, cts.Token);
                await CompleteAsync(_downloadContentDir);
            }
            catch (OperationCanceledException)
            {
                // The user cancelled; return the dialog to its initial choices.
                Progress = 0;
            }
            catch (Exception ex)
            {
                ErrorMessage = Localizer.Get("Dialog.ContentSetup.Error.DownloadFailed") + ex.Message;
            }
            finally
            {
                _downloadCts = null;
                IsBusy = false;
            }
        }

        /// <summary>Requests cancellation of an in-progress download; a no-op when nothing is downloading.</summary>
        public void CancelDownload()
        {
            _downloadCts?.Cancel();
        }

        /// <summary>Validates a user-picked folder; on success saves and completes, otherwise sets an error.</summary>
        public async Task ApplyLocatedFolder(string dir)
        {
            if (!ContentLocation.IsValid(dir))
            {
                ErrorMessage = Localizer.Get("Dialog.ContentSetup.Error.InvalidFolder");
                return;
            }
            await CompleteAsync(dir);
        }

        private async Task CompleteAsync(string contentDir)
        {
            await _onInstalled(contentDir);
            Completed?.Invoke();
        }
    }
}
