using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>Installs content into a directory on the local filesystem (desktop).</summary>
    public sealed class FolderContentInstaller(string destContentDir) : IContentInstaller
    {
        /// <inheritdoc />
        public Task InstallFromDownloadAsync(IProgress<InstallProgress>? progress, CancellationToken ct)
        {
            return ContentDownloader.DownloadAsync(destContentDir, progress, ct);
        }

        /// <inheritdoc />
        public Task InstallFromZipAsync(Stream zipStream, CancellationToken ct)
        {
            // Extraction and per-file hash verification are synchronous and CPU/IO-bound; run them
            // on a background thread so the setup dialog's UI thread stays responsive.
            return Task.Run(() =>
            {
                _ = Directory.CreateDirectory(destContentDir);
                using (ZipArchive zip = new(zipStream, ZipArchiveMode.Read, leaveOpen: true))
                {
                    zip.ExtractToDirectory(destContentDir, overwriteFiles: true);
                }
                IReadOnlyList<string> invalid = ContentLocation.FindInvalidFiles(destContentDir);
                if (invalid.Count > 0)
                {
                    throw new InvalidDataException(
                        $"The provided asset bundle is incomplete or corrupt. Invalid files: {ContentManifest.SummarizeInvalidFiles(invalid)}");
                }
            }, ct);
        }
    }
}
