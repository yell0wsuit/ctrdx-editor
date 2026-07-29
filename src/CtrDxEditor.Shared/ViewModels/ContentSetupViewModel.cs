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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDownloading))]
        [NotifyPropertyChangedFor(nameof(ShowDownloadSizeLabel))]
        public partial bool IsBusy { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDownloading))]
        public partial bool IsInstallingZip { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDownloading))]
        public partial bool IsVerifying { get; set; }

        [ObservableProperty] public partial double Progress { get; set; }
        [ObservableProperty] public partial string? ErrorMessage { get; set; }

        /// <summary>True while bytes are transferring (determinate progress): busy, but not installing a picked zip or verifying a finished download.</summary>
        public bool IsDownloading => IsBusy && !IsInstallingZip && !IsVerifying;

        /// <summary>Whether the Quit button is shown (desktop can quit the app; the browser never does).</summary>
        public bool AllowQuit { get; }

        /// <summary>
        /// Whether this is a re-download of outdated content rather than first-run setup.
        /// </summary>
        /// <remarks>
        /// The two share every mechanism - download, manual download, zip upload, progress, verification,
        /// error reporting - and differ only in why the user is here and whether they may walk away. Two
        /// dialogs would mean asking twice for one download.
        /// </remarks>
        public bool IsUpdate { get; }

        /// <summary>Heading shown while idle, naming the reason the dialog is open.</summary>
        public string Title => Localizer.Get(IsUpdate ? "Dialog.AssetUpdate.Header" : "Dialog.ContentSetup.Title");

        /// <summary>Body shown while idle, describing the choice.</summary>
        public string Description => Localizer.Get(IsUpdate ? "Dialog.AssetUpdate.Body" : "Dialog.ContentSetup.Description");

        /// <summary>Whether the setup dialog may be dismissed: immediately on desktop, or after browser setup completes.</summary>
        public bool CanDismiss { get; private set; }

        /// <summary>Whether the in-app Download button is shown; false where a cross-origin fetch is blocked (the browser), leaving only manual download and zip upload.</summary>
        public bool AllowDownload { get; }

        /// <summary>Whether the Download Manually button is shown (opens the bundle's direct URL in a new tab / the default browser).</summary>
        public bool AllowManualDownload { get; }

        /// <summary>Direct download URL opened in the browser when the user clicks Download Manually.</summary>
        public string ManualDownloadUrl { get; }

        /// <summary>Approximate size of the platform's asset bundle (e.g. "336 MB"), or empty to show nothing.</summary>
        public string DownloadSizeLabel { get; }

        /// <summary>Whether the download-size disclosure should be shown: idle, direct download available, and a label was supplied.</summary>
        public bool ShowDownloadSizeLabel => !IsBusy && AllowDownload && !string.IsNullOrEmpty(DownloadSizeLabel);

        /// <summary>Raised once content setup succeeds, so the view can close.</summary>
        public event Action? Completed;

        /// <summary>Command that downloads and installs the default asset bundle.</summary>
        public IAsyncRelayCommand DownloadCommand { get; }

        /// <summary>Creates a content setup view model.</summary>
        public ContentSetupViewModel(
            IContentInstaller installer, Func<Task> onInstalled,
            bool allowQuit = true, bool allowManualDownload = true, bool allowDownload = true,
            string downloadSizeLabel = "", string manualDownloadUrl = ContentDownloader.AssetsUrl,
            bool? canDismiss = null, bool isUpdate = false)
        {
            _installer = installer;
            _onInstalled = onInstalled;
            AllowQuit = allowQuit;
            IsUpdate = isUpdate;
            // Dismissal normally tracks the Quit button: first-run setup on the browser can offer
            // neither, because the editor cannot run without content. Re-downloading an outdated bundle
            // is the case that needs them apart - the editor is already running and the user must be
            // able to back out, but quitting is not the way out of an optional download.
            CanDismiss = canDismiss ?? allowQuit;
            AllowManualDownload = allowManualDownload;
            AllowDownload = allowDownload;
            DownloadSizeLabel = downloadSizeLabel;
            ManualDownloadUrl = manualDownloadUrl;
            // AsyncRelayCommand disallows concurrent executions by default, so re-clicks are ignored while busy.
            DownloadCommand = new AsyncRelayCommand(DownloadAsync);
        }

        private async Task DownloadAsync()
        {
            using CancellationTokenSource cts = new();
            _downloadCts = cts;
            IsBusy = true;
            IsInstallingZip = false;
            IsVerifying = false;
            ErrorMessage = null;
            Progress = 0;
            try
            {
                Progress<InstallProgress> progress = new(p =>
                {
                    if (p.Stage == InstallStage.Verifying)
                    {
                        IsVerifying = true;
                    }
                    else
                    {
                        Progress = p.Fraction;
                    }
                });
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
                ErrorMessage = $"{Localizer.Get("Dialog.ContentSetup.Error.DownloadFailed")}\n{ex.Message}";
            }
            finally
            {
                _downloadCts = null;
                IsBusy = false;
                IsVerifying = false;
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
            IsInstallingZip = true;
            ErrorMessage = null;
            try
            {
                await _installer.InstallFromZipAsync(zip, CancellationToken.None);
                await CompleteAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"{Localizer.Get("Dialog.ContentSetup.Error.InvalidFolder")}\n{ex.Message}";
            }
            finally
            {
                IsBusy = false;
                IsInstallingZip = false;
            }
        }

        private async Task CompleteAsync()
        {
            await _onInstalled();
            CanDismiss = true;
            Completed?.Invoke();
        }
    }
}
