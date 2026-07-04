using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for the first-run content setup view model.</summary>
    public class ContentSetupViewModelTests
    {
        private sealed class FakeInstaller(
            Func<IProgress<InstallProgress>?, CancellationToken, Task>? download = null,
            Func<Stream, CancellationToken, Task>? zip = null) : IContentInstaller
        {
            public Task InstallFromDownloadAsync(IProgress<InstallProgress>? progress, CancellationToken ct)
            {
                return download is null ? Task.CompletedTask : download(progress, ct);
            }

            public Task InstallFromZipAsync(Stream zipStream, CancellationToken ct)
            {
                return zip is null ? Task.CompletedTask : zip(zipStream, ct);
            }
        }

        /// <summary>Verifies allowQuit/allowManualDownload default to true, matching today's always-shown buttons.</summary>
        [Fact]
        public void AllowQuitAndAllowManualDownloadDefaultToTrue()
        {
            ContentSetupViewModel vm = new(new FakeInstaller(), () => Task.CompletedTask);

            Assert.True(vm.AllowQuit);
            Assert.True(vm.AllowManualDownload);
        }

        /// <summary>Verifies allowQuit/allowManualDownload can each be disabled independently (the browser's setup).</summary>
        [Fact]
        public void AllowQuitAndAllowManualDownloadCanBeDisabledIndependently()
        {
            ContentSetupViewModel vm = new(
                new FakeInstaller(), () => Task.CompletedTask, allowQuit: false, allowManualDownload: true);

            Assert.False(vm.AllowQuit);
            Assert.True(vm.AllowManualDownload);
        }

        /// <summary>Verifies the download-size disclosure is hidden by default (no label supplied).</summary>
        [Fact]
        public void ShowDownloadSizeLabelIsFalseWhenNoLabelSupplied()
        {
            ContentSetupViewModel vm = new(new FakeInstaller(), () => Task.CompletedTask);

            Assert.False(vm.ShowDownloadSizeLabel);
            Assert.Equal(string.Empty, vm.DownloadSizeLabel);
        }

        /// <summary>Verifies the download-size disclosure shows while idle when supplied, and hides while busy.</summary>
        [Fact]
        public void ShowDownloadSizeLabelReflectsSuppliedLabelAndBusyState()
        {
            ContentSetupViewModel vm = new(
                new FakeInstaller(), () => Task.CompletedTask, downloadSizeLabel: "25.1 MB");

            Assert.Equal("25.1 MB", vm.DownloadSizeLabel);
            Assert.True(vm.ShowDownloadSizeLabel);

            vm.IsBusy = true;
            Assert.False(vm.ShowDownloadSizeLabel);
        }

        /// <summary>Verifies that cancelling an active download clears busy state without an error.</summary>
        [Fact]
        public async Task CancelDownloadStopsTheDownloadWithoutError()
        {
            // A download that only completes when its cancellation token fires.
            ContentSetupViewModel vm = new(
                new FakeInstaller(download: (p, ct) => Task.Delay(Timeout.Infinite, ct)),
                () => Task.CompletedTask);

            Task run = vm.DownloadCommand.ExecuteAsync(null);
            Assert.True(vm.IsBusy);

            vm.CancelDownload();
            await run;

            Assert.False(vm.IsBusy);
            Assert.Null(vm.ErrorMessage);
        }

        /// <summary>Verifies IsDownloading (not IsInstallingZip) is set while a download is in progress, and clears after.</summary>
        [Fact]
        public async Task DownloadCommandSetsIsDownloadingWhileRunning()
        {
            ContentSetupViewModel vm = new(
                new FakeInstaller(download: (p, ct) => Task.Delay(Timeout.Infinite, ct)),
                () => Task.CompletedTask);

            Task run = vm.DownloadCommand.ExecuteAsync(null);
            Assert.True(vm.IsDownloading);
            Assert.False(vm.IsInstallingZip);

            vm.CancelDownload();
            await run;

            Assert.False(vm.IsDownloading);
            Assert.False(vm.IsInstallingZip);
        }

        /// <summary>Verifies the install reporting a Verifying stage flips the VM from downloading to the indeterminate verifying view, then clears.</summary>
        [Fact]
        public async Task DownloadReportingVerifyingStageSwitchesToVerifyingView()
        {
            TaskCompletionSource gate = new();
            TaskCompletionSource verifyingSeen = new();
            ContentSetupViewModel vm = new(
                new FakeInstaller(download: async (p, ct) =>
                {
                    p?.Report(new InstallProgress(InstallStage.Verifying, 0));
                    await gate.Task;
                }),
                () => Task.CompletedTask);
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.IsVerifying) && vm.IsVerifying)
                {
                    _ = verifyingSeen.TrySetResult();
                }
            };

            Task run = vm.DownloadCommand.ExecuteAsync(null);
            // Progress<T> posts its callback asynchronously; wait for the flip rather than racing it.
            await verifyingSeen.Task;
            Assert.True(vm.IsVerifying);
            Assert.False(vm.IsDownloading);

            gate.SetResult();
            await run;

            Assert.False(vm.IsVerifying);
            Assert.False(vm.IsBusy);
        }

        /// <summary>Verifies IsInstallingZip (not IsDownloading) is set while a picked zip is being installed, and clears after.</summary>
        [Fact]
        public async Task InstallFromZipAsyncSetsIsInstallingZipWhileRunning()
        {
            TaskCompletionSource gate = new();
            ContentSetupViewModel vm = new(
                new FakeInstaller(zip: async (s, ct) => await gate.Task),
                () => Task.CompletedTask);

            using MemoryStream zip = new();
            Task run = vm.InstallFromZipAsync(zip);

            Assert.True(vm.IsBusy);
            Assert.True(vm.IsInstallingZip);
            Assert.False(vm.IsDownloading);

            gate.SetResult();
            await run;

            Assert.False(vm.IsBusy);
            Assert.False(vm.IsInstallingZip);
        }

        /// <summary>Verifies that a successful download runs the completion callback and raises Completed.</summary>
        [Fact]
        public async Task DownloadCommandSuccessRunsOnInstalledAndRaisesCompleted()
        {
            bool installed = false;
            bool completed = false;

            static Task Fake(IProgress<InstallProgress>? p, CancellationToken ct)
            {
                p?.Report(new InstallProgress(InstallStage.Downloading, 1.0));
                return Task.CompletedTask;
            }

            ContentSetupViewModel vm = new(
                new FakeInstaller(download: Fake),
                () => { installed = true; return Task.CompletedTask; });
            vm.Completed += () => completed = true;

            await vm.DownloadCommand.ExecuteAsync(null);

            Assert.True(installed);
            Assert.True(completed);
            Assert.Null(vm.ErrorMessage);
        }

        /// <summary>Verifies that a failed download reports an error and never runs the completion callback.</summary>
        [Fact]
        public async Task DownloadCommandFailureSetsErrorAndSkipsOnInstalled()
        {
            bool installed = false;
            ContentSetupViewModel vm = new(
                new FakeInstaller(download: (p, ct) => throw new InvalidOperationException("boom")),
                () => { installed = true; return Task.CompletedTask; });

            await vm.DownloadCommand.ExecuteAsync(null);

            Assert.False(installed);
            Assert.NotNull(vm.ErrorMessage);
            Assert.Contains("boom", vm.ErrorMessage);
        }

        /// <summary>Verifies that installing from an uploaded zip runs the completion callback and raises Completed.</summary>
        [Fact]
        public async Task InstallFromZipAsyncSuccessRunsOnInstalledAndRaisesCompleted()
        {
            bool installed = false;
            bool completed = false;
            Stream? seen = null;

            ContentSetupViewModel vm = new(
                new FakeInstaller(zip: (s, ct) => { seen = s; return Task.CompletedTask; }),
                () => { installed = true; return Task.CompletedTask; });
            vm.Completed += () => completed = true;

            using MemoryStream zip = new();
            await vm.InstallFromZipAsync(zip);

            Assert.Same(zip, seen);
            Assert.True(installed);
            Assert.True(completed);
            Assert.False(vm.IsBusy);
            Assert.Null(vm.ErrorMessage);
        }

        /// <summary>Verifies that a failed zip install reports an error and never runs the completion callback.</summary>
        [Fact]
        public async Task InstallFromZipAsyncFailureSetsErrorAndSkipsOnInstalled()
        {
            bool installed = false;
            ContentSetupViewModel vm = new(
                new FakeInstaller(zip: (s, ct) => throw new InvalidOperationException("bad zip")),
                () => { installed = true; return Task.CompletedTask; });

            using MemoryStream zip = new();
            await vm.InstallFromZipAsync(zip);

            Assert.False(installed);
            Assert.False(vm.IsBusy);
            Assert.NotNull(vm.ErrorMessage);
            Assert.Contains("bad zip", vm.ErrorMessage);
        }
    }
}
