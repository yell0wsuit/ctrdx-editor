using System;
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
            "https://github.com/yell0wsuit/ctrdx-assets/releases/latest/download/ctrdx-assets.zip";

        /// <summary>Release page users can open when they want to download assets manually.</summary>
        public const string ReleasesPageUrl =
            "https://github.com/yell0wsuit/ctrdx-assets/releases/";

        /// <summary>
        /// Downloads the asset bundle and extracts it into <paramref name="destContentDir"/>.
        /// <paramref name="progress"/> receives fractional progress in [0, 1] when the server reports
        /// a content length. Throws <see cref="InvalidDataException"/> if the result is not valid content.
        /// </summary>
        public static async Task DownloadAsync(
            string destContentDir, IProgress<double>? progress, CancellationToken ct)
        {
            string tmp = Path.Combine(Path.GetTempPath(), "ctrdx-assets-" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(tmp);
            try
            {
                string zipPath = Path.Combine(tmp, "ctrdx-assets.zip");
                await DownloadFileAsync(AssetsUrl, zipPath, progress, ct);
                ExtractInto(zipPath, destContentDir);
                if (!ContentLocation.IsValid(destContentDir))
                {
                    throw new InvalidDataException(
                        "The downloaded asset bundle is incomplete or corrupt.");
                }
            }
            finally
            {
                try { Directory.Delete(tmp, recursive: true); }
                catch (IOException) { /* best-effort cleanup */ }
            }
        }

        /// <summary>Extracts a downloaded asset zip into <paramref name="destContentDir"/>, overwriting existing files.</summary>
        public static void ExtractInto(string zipPath, string destContentDir)
        {
            _ = Directory.CreateDirectory(destContentDir);
            ZipFile.ExtractToDirectory(zipPath, destContentDir, overwriteFiles: true);
        }

        private static async Task DownloadFileAsync(
            string url, string dest, IProgress<double>? progress, CancellationToken ct)
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
                    progress?.Report((double)readTotal / total.Value);
                }
            }
        }
    }
}
