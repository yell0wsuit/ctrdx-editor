using System;
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
        public Task InstallFromDownloadAsync(IProgress<double>? progress, CancellationToken ct)
        {
            return ContentDownloader.DownloadAsync(destContentDir, progress, ct);
        }

        /// <inheritdoc />
        public async Task InstallFromZipAsync(Stream zipStream, CancellationToken ct)
        {
            _ = Directory.CreateDirectory(destContentDir);
            using (ZipArchive zip = new(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                zip.ExtractToDirectory(destContentDir, overwriteFiles: true);
            }
            if (!ContentLocation.IsValid(destContentDir))
            {
                throw new InvalidDataException("The provided asset bundle is incomplete or corrupt.");
            }
            await Task.CompletedTask;
        }
    }
}
