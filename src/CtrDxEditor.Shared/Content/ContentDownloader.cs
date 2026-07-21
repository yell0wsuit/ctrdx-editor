using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>Downloads and extracts the binary content-asset bundle from the ctrdx-assets release.</summary>
    public static class ContentDownloader
    {
        /// <summary>Direct URL for the latest downloadable asset bundle.</summary>
        public const string AssetsUrl =
            "https://github.com/yell0wsuit/ctrdx-assets/releases/latest/download/ctrdx-assets-vk.zip";

        /// <summary>Direct URL for the browser's WebP-sprite asset bundle (smaller download than the desktop bundle).</summary>
        public const string WebpAssetsUrl =
            "https://github.com/yell0wsuit/ctrdx-assets/releases/latest/download/ctrdx-webp.zip";

        /// <summary>
        /// Downloads the asset bundle and installs it into <paramref name="destContentDir"/> for the
        /// <paramref name="imageExtension"/> platform. <paramref name="progress"/> receives the current
        /// stage and, while downloading, fractional progress in [0, 1] when the server reports a content
        /// length. The install is atomic: content is extracted and validated in a sibling staging folder
        /// and only swapped in on success, so a corrupt or wrong-platform bundle leaves
        /// <paramref name="destContentDir"/> untouched. Throws <see cref="InvalidDataException"/> when the
        /// downloaded bundle is not valid content for this platform.
        /// </summary>
        /// <param name="destContentDir">The content directory to install into. Left untouched unless the install succeeds.</param>
        /// <param name="imageExtension">The platform's image extension, such as <c>.png</c> or <c>.webp</c>; content built for another platform is rejected.</param>
        /// <param name="progress">Receives stage changes, and fractional progress while downloading. May be null.</param>
        /// <param name="ct">Cancels the download; staging is cleaned up on the way out.</param>
        public static async Task DownloadAsync(
            string destContentDir, string imageExtension, IProgress<InstallProgress>? progress, CancellationToken ct)
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"ctrdx-assets-{Guid.NewGuid():N}");
            // Staging sits next to the destination (same volume) so the final swap is a rename.
            string staging = $"{destContentDir}.staging-{Guid.NewGuid():N}";
            _ = Directory.CreateDirectory(tmp);
            try
            {
                string zipPath = Path.Combine(tmp, "ctrdx-assets.zip");
                await DownloadFileAsync(AssetsUrl, zipPath, progress, ct);
                // Bytes are in; the remaining extract + hash-verify has no byte-level progress, so
                // switch the dialog to its indeterminate "verifying" state before starting it.
                progress?.Report(new InstallProgress(InstallStage.Verifying, 0));
                // Extraction and per-file hash verification are synchronous and CPU/IO-bound; run
                // them on a background thread so the setup dialog's UI thread stays responsive while
                // a large bundle is unpacked and checked.
                await Task.Run(() =>
                {
                    ExtractInto(zipPath, staging);
                    CommitStagedContent(staging, destContentDir, imageExtension);
                }, ct);
            }
            finally
            {
                try { Directory.Delete(tmp, recursive: true); }
                catch (IOException) { /* best-effort cleanup */ }
                // A successful commit renames staging away; anything left is a failed attempt to discard.
                if (Directory.Exists(staging))
                {
                    try { Directory.Delete(staging, recursive: true); }
                    catch (IOException) { /* best-effort cleanup */ }
                }
            }
        }

        /// <summary>
        /// Validates freshly-extracted content in <paramref name="stagingDir"/> and, if valid, atomically
        /// replaces <paramref name="destContentDir"/> with it. Throws <see cref="InvalidDataException"/>
        /// (leaving the destination untouched) when the staged content is incomplete, corrupt, or built
        /// for a different platform than <paramref name="imageExtension"/>.
        /// </summary>
        /// <param name="stagingDir">The freshly-extracted content to validate. Renamed onto the destination on success.</param>
        /// <param name="destContentDir">The content directory to replace. Must be on the same volume as <paramref name="stagingDir"/> so the swap is a rename.</param>
        /// <param name="imageExtension">The platform's image extension, such as <c>.png</c> or <c>.webp</c>; content built for another platform is rejected.</param>
        public static void CommitStagedContent(string stagingDir, string destContentDir, string imageExtension)
        {
            IReadOnlyList<string> invalid = ContentLocation.FindInvalidFiles(stagingDir, imageExtension);
            if (invalid.Count > 0)
            {
                throw new InvalidDataException(
                    $"The asset bundle is incomplete or corrupt. Invalid files:\n{ContentManifest.SummarizeInvalidFiles(invalid)}");
            }
            if (Directory.Exists(destContentDir))
            {
                Directory.Delete(destContentDir, recursive: true);
            }
            Directory.Move(stagingDir, destContentDir);
        }

        /// <summary>Extracts a downloaded asset zip into <paramref name="destContentDir"/>, overwriting existing files.</summary>
        /// <param name="zipPath">The downloaded asset zip.</param>
        /// <param name="destContentDir">The directory to extract into; created when absent.</param>
        public static void ExtractInto(string zipPath, string destContentDir)
        {
            _ = Directory.CreateDirectory(destContentDir);
            ZipFile.ExtractToDirectory(zipPath, destContentDir, overwriteFiles: true);
        }

        private static async Task DownloadFileAsync(
            string url, string dest, IProgress<InstallProgress>? progress, CancellationToken ct)
        {
            using HttpClient http = new() { Timeout = TimeSpan.FromMinutes(30) };
            // GitHub rejects requests without a User-Agent.
            http.DefaultRequestHeaders.UserAgent.ParseAdd("CtrDxEditor-ContentDownloader/1.0");

            using HttpResponseMessage resp =
                await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            _ = resp.EnsureSuccessStatusCode();

            long? total = resp.Content.Headers.ContentLength;
            await using Stream src = await resp.Content.ReadAsStreamAsync(ct);
            await using FileStream fs = File.Create(dest);

            byte[] buffer = new byte[81920];
            long readTotal = 0;
            int n;
            while ((n = await src.ReadAsync(buffer, ct)) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, n), ct);
                readTotal += n;
                if (total is > 0)
                {
                    progress?.Report(new InstallProgress(InstallStage.Downloading, (double)readTotal / total.Value));
                }
            }
        }
    }
}
