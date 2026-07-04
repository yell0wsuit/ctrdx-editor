using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CtrDxEditor.Content;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Backs the first-run Content Setup dialog: download the asset bundle or upload a zip file.</summary>
    public sealed partial class ContentSetupViewModel : ViewModelBase
    {
        private readonly IContentInstaller _installer;
        private readonly Func<Task> _onInstalled;
        private CancellationTokenSource? _downloadCts;

        [ObservableProperty] public partial bool IsBusy { get; set; }
        [ObservableProperty] public partial double Progress { get; set; }
        [ObservableProperty] public partial string? ErrorMessage { get; set; }

        /// <summary>Whether the Quit button is shown (desktop can quit the app; the browser never does).</summary>
        public bool AllowQuit { get; }

        /// <summary>Whether the Download Manually button is shown (desktop opens the releases page manually).</summary>
        public bool AllowManualDownload { get; }

        /// <summary>Raised once content setup succeeds, so the view can close.</summary>
        public event Action? Completed;

        /// <summary>Command that downloads and installs the default asset bundle.</summary>
        public IAsyncRelayCommand DownloadCommand { get; }

        /// <summary>Creates a content setup view model.</summary>
        public ContentSetupViewModel(
            IContentInstaller installer, Func<Task> onInstalled, bool allowQuit = true, bool allowManualDownload = true)
        {
            _installer = installer;
            _onInstalled = onInstalled;
            AllowQuit = allowQuit;
            AllowManualDownload = allowManualDownload;
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
                await CompleteAsync();
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

        /// <summary>Installs from a user-picked zip stream (CORS-proof fallback).</summary>
        public async Task InstallFromZipAsync(Stream zip)
        {
            IsBusy = true;
            ErrorMessage = null;
            try
            {
                await _installer.InstallFromZipAsync(zip, CancellationToken.None);
                await CompleteAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = Localizer.Get("Dialog.ContentSetup.Error.InvalidFolder") + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CompleteAsync()
        {
            await _onInstalled();
            Completed?.Invoke();
        }
    }
}
