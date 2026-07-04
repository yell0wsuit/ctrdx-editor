using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>Installs content into a directory on the local filesystem (desktop), for the given sprite image extension (e.g. ".png").</summary>
    public sealed class FolderContentInstaller(string destContentDir, string imageExtension) : IContentInstaller
    {
        /// <inheritdoc />
        public Task InstallFromDownloadAsync(IProgress<InstallProgress>? progress, CancellationToken ct)
        {
            return ContentDownloader.DownloadAsync(destContentDir, imageExtension, progress, ct);
        }

        /// <inheritdoc />
        public Task InstallFromZipAsync(Stream zipStream, CancellationToken ct)
        {
            // Extraction and per-file hash verification are synchronous and CPU/IO-bound; run them
            // on a background thread so the setup dialog's UI thread stays responsive.
            return Task.Run(() =>
            {
                // Extract into a sibling staging folder and only swap it in once validated, so a
                // corrupt or wrong-platform zip never leaves broken content in destContentDir.
                string staging = $"{destContentDir}.staging-{Guid.NewGuid():N}";
                try
                {
                    _ = Directory.CreateDirectory(staging);
                    using (ZipArchive zip = new(zipStream, ZipArchiveMode.Read, leaveOpen: true))
                    {
                        zip.ExtractToDirectory(staging, overwriteFiles: true);
                    }
                    ContentDownloader.CommitStagedContent(staging, destContentDir, imageExtension);
                }
                finally
                {
                    // A successful commit renames staging away; anything left is a failed attempt.
                    if (Directory.Exists(staging))
                    {
                        try { Directory.Delete(staging, recursive: true); }
                        catch (IOException) { /* best-effort cleanup */ }
                    }
                }
            }, ct);
        }
    }
}
