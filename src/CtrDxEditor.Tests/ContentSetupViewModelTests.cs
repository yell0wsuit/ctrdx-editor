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
            Func<IProgress<double>?, CancellationToken, Task>? download = null,
            Func<Stream, CancellationToken, Task>? zip = null) : IContentInstaller
        {
            public Task InstallFromDownloadAsync(IProgress<double>? progress, CancellationToken ct)
            {
                return download is null ? Task.CompletedTask : download(progress, ct);
            }

            public Task InstallFromZipAsync(Stream zipStream, CancellationToken ct)
            {
                return zip is null ? Task.CompletedTask : zip(zipStream, ct);
            }
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

        /// <summary>Verifies that a successful download runs the completion callback and raises Completed.</summary>
        [Fact]
        public async Task DownloadCommandSuccessRunsOnInstalledAndRaisesCompleted()
        {
            bool installed = false;
            bool completed = false;

            static Task Fake(IProgress<double>? p, CancellationToken ct)
            {
                p?.Report(1.0);
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
