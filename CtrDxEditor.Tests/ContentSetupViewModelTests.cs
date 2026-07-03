using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    public class ContentSetupViewModelTests
    {
        private static void WriteValidContent(string dir)
        {
            _ = Directory.CreateDirectory(Path.Combine(dir, "images"));
            File.WriteAllText(Path.Combine(dir, "images", "a.png"), "x");
            File.WriteAllText(
                Path.Combine(dir, ContentManifest.FileName),
                                     /*lang=json,strict*/
                                     """{"files":{"images/a.png":"_"}}""");
        }

        [Fact]
        public async Task CancelDownload_stops_the_download_without_error()
        {
            // A download that only completes when its cancellation token fires.
            ContentSetupViewModel vm = new(
                "/unused",
                (d, p, ct) => Task.Delay(Timeout.Infinite, ct),
                _ => { });

            Task run = vm.DownloadCommand.ExecuteAsync(null);
            Assert.True(vm.IsBusy);

            vm.CancelDownload();
            await run;

            Assert.False(vm.IsBusy);
            Assert.Null(vm.ResolvedContentPath);
            Assert.Null(vm.ErrorMessage);
        }

        [Fact]
        public async Task DownloadCommand_success_sets_resolved_path_and_raises_completed()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-vm-").FullName;
            try
            {
                string dest = Path.Combine(root, "content");
                string? saved = null;
                bool completed = false;

                static Task Fake(string d, IProgress<double> p, CancellationToken ct)
                {
                    WriteValidContent(d);
                    p.Report(1.0);
                    return Task.CompletedTask;
                }

                ContentSetupViewModel vm = new(dest, Fake, path => saved = path);
                vm.Completed += () => completed = true;

                await vm.DownloadCommand.ExecuteAsync(null);

                Assert.Equal(dest, vm.ResolvedContentPath);
                Assert.Equal(dest, saved);
                Assert.True(completed);
                Assert.Null(vm.ErrorMessage);
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        [Fact]
        public async Task DownloadCommand_failure_sets_error_and_leaves_path_null()
        {
            ContentSetupViewModel vm = new(
                "/unused",
                (d, p, ct) => throw new InvalidOperationException("boom"),
                _ => { });

            await vm.DownloadCommand.ExecuteAsync(null);

            Assert.Null(vm.ResolvedContentPath);
            Assert.NotNull(vm.ErrorMessage);
            Assert.Contains("boom", vm.ErrorMessage);
        }

        [Fact]
        public void ApplyLocatedFolder_invalid_sets_error()
        {
            string dir = Directory.CreateTempSubdirectory("ctrdx-vm-").FullName;
            try
            {
                ContentSetupViewModel vm = new("/unused", (d, p, ct) => Task.CompletedTask, _ => { });

                vm.ApplyLocatedFolder(dir); // empty dir -> invalid

                Assert.NotNull(vm.ErrorMessage);
                Assert.Null(vm.ResolvedContentPath);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [Fact]
        public void ApplyLocatedFolder_valid_saves_and_completes()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-vm-").FullName;
            try
            {
                string dir = Path.Combine(root, "content");
                WriteValidContent(dir);
                string? saved = null;
                ContentSetupViewModel vm = new("/unused", (d, p, ct) => Task.CompletedTask, p => saved = p);

                vm.ApplyLocatedFolder(dir);

                Assert.Equal(dir, vm.ResolvedContentPath);
                Assert.Equal(dir, saved);
                Assert.Null(vm.ErrorMessage);
            }
            finally { Directory.Delete(root, recursive: true); }
        }
    }
}
