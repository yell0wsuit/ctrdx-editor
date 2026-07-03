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
            Func<IProgress<double>?, CancellationToken, Task> download) : IContentInstaller
        {
            public Task InstallFromDownloadAsync(IProgress<double>? progress, CancellationToken ct)
            {
                return download(progress, ct);
            }

            public Task InstallFromZipAsync(Stream zipStream, CancellationToken ct)
            {
                return Task.CompletedTask;
            }
        }

        private static void WriteValidContent(string dir)
        {
            _ = Directory.CreateDirectory(Path.Combine(dir, "images"));
            File.WriteAllText(Path.Combine(dir, "images", "a.png"), "x");
            File.WriteAllText(
                Path.Combine(dir, ContentManifest.FileName),
                                     /*lang=json,strict*/
                                     """{"files":{"images/a.png":"_"}}""");
        }

        /// <summary>Verifies that cancelling an active download clears busy state without an error.</summary>
        [Fact]
        public async Task CancelDownloadStopsTheDownloadWithoutError()
        {
            // A download that only completes when its cancellation token fires.
            ContentSetupViewModel vm = new(
                new FakeInstaller((p, ct) => Task.Delay(Timeout.Infinite, ct)),
                "/unused",
                _ => Task.CompletedTask);

            Task run = vm.DownloadCommand.ExecuteAsync(null);
            Assert.True(vm.IsBusy);

            vm.CancelDownload();
            await run;

            Assert.False(vm.IsBusy);
            Assert.Null(vm.ErrorMessage);
        }

        /// <summary>Verifies that a successful download completes with the download destination.</summary>
        [Fact]
        public async Task DownloadCommandSuccessRaisesCompletedWithDownloadPath()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-vm-").FullName;
            try
            {
                string dest = Path.Combine(root, "content");
                string? saved = null;
                bool completed = false;

                Task Fake(IProgress<double>? p, CancellationToken ct)
                {
                    WriteValidContent(dest);
                    p?.Report(1.0);
                    return Task.CompletedTask;
                }

                ContentSetupViewModel vm = new(
                    new FakeInstaller(Fake),
                    dest,
                    path => { saved = path; return Task.CompletedTask; });
                vm.Completed += () => completed = true;

                await vm.DownloadCommand.ExecuteAsync(null);

                Assert.Equal(dest, saved);
                Assert.True(completed);
                Assert.Null(vm.ErrorMessage);
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies that a failed download reports an error and leaves setup unresolved.</summary>
        [Fact]
        public async Task DownloadCommandFailureSetsErrorAndLeavesPathNull()
        {
            string? saved = null;
            ContentSetupViewModel vm = new(
                new FakeInstaller((p, ct) => throw new InvalidOperationException("boom")),
                "/unused",
                path => { saved = path; return Task.CompletedTask; });

            await vm.DownloadCommand.ExecuteAsync(null);

            Assert.Null(saved);
            Assert.NotNull(vm.ErrorMessage);
            Assert.Contains("boom", vm.ErrorMessage);
        }

        /// <summary>Verifies that locating an invalid folder records an error.</summary>
        [Fact]
        public async Task ApplyLocatedFolderInvalidSetsError()
        {
            string dir = Directory.CreateTempSubdirectory("ctrdx-vm-").FullName;
            try
            {
                string? saved = null;
                ContentSetupViewModel vm = new(
                    new FakeInstaller((p, ct) => Task.CompletedTask),
                    "/unused",
                    path => { saved = path; return Task.CompletedTask; });

                await vm.ApplyLocatedFolder(dir); // empty dir -> invalid

                Assert.NotNull(vm.ErrorMessage);
                Assert.Null(saved);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        /// <summary>Verifies that locating a valid folder completes with the located folder.</summary>
        [Fact]
        public async Task ApplyLocatedFolderValidSavesAndCompletes()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-vm-").FullName;
            try
            {
                string dir = Path.Combine(root, "content");
                WriteValidContent(dir);
                string? saved = null;
                ContentSetupViewModel vm = new(
                    new FakeInstaller((p, ct) => Task.CompletedTask),
                    "/unused",
                    path => { saved = path; return Task.CompletedTask; });

                await vm.ApplyLocatedFolder(dir);

                Assert.Equal(dir, saved);
                Assert.Null(vm.ErrorMessage);
            }
            finally { Directory.Delete(root, recursive: true); }
        }
    }
}
