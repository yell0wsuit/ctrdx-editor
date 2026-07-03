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
        private readonly string _defaultContentDir;
        private readonly Func<string, IProgress<double>, CancellationToken, Task> _download;
        private readonly Action<string> _saveContentPath;
        private CancellationTokenSource? _downloadCts;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private double _progress;
        [ObservableProperty] private string? _errorMessage;

        /// <summary>The resolved content directory once setup succeeds; null until then.</summary>
        public string? ResolvedContentPath { get; private set; }

        /// <summary>Raised once <see cref="ResolvedContentPath"/> is set, so the view can close.</summary>
        public event Action? Completed;

        public IAsyncRelayCommand DownloadCommand { get; }

        public ContentSetupViewModel(
            string defaultContentDir,
            Func<string, IProgress<double>, CancellationToken, Task> download,
            Action<string> saveContentPath)
        {
            _defaultContentDir = defaultContentDir;
            _download = download;
            _saveContentPath = saveContentPath;
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
                await _download(_defaultContentDir, progress, cts.Token);
                Succeed(_defaultContentDir);
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
        public void CancelDownload() => _downloadCts?.Cancel();

        /// <summary>Validates a user-picked folder; on success saves and completes, otherwise sets an error.</summary>
        public void ApplyLocatedFolder(string dir)
        {
            if (!ContentLocation.IsValid(dir))
            {
                ErrorMessage = Localizer.Get("Dialog.ContentSetup.Error.InvalidFolder");
                return;
            }
            Succeed(dir);
        }

        private void Succeed(string contentDir)
        {
            _saveContentPath(contentDir);
            ResolvedContentPath = contentDir;
            Completed?.Invoke();
        }
    }
}
