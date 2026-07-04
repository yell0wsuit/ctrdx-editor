using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CtrDxEditor.Content;

namespace CtrDxEditor.Browser.Content
{
    /// <summary>Installs the content bundle into IndexedDB (download via fetch, or uploaded zip).</summary>
    public sealed class BrowserContentInstaller : IContentInstaller
    {
        /// <inheritdoc />
        public async Task InstallFromDownloadAsync(IProgress<double>? progress, CancellationToken ct)
        {
            using HttpClient http = new();
            // Browser HttpClient goes through fetch; progress is coarse (no reliable content-length on redirects).
            byte[] bytes = await http.GetByteArrayAsync(ContentDownloader.WebpAssetsUrl, ct);
            progress?.Report(1.0);
            await StoreAsync(bytes);
        }

        /// <inheritdoc />
        public async Task InstallFromZipAsync(Stream zipStream, CancellationToken ct)
        {
            using MemoryStream ms = new();
            await zipStream.CopyToAsync(ms, ct);
            await StoreAsync(ms.ToArray());
        }

        private static async Task StoreAsync(byte[] zipBytes)
        {
            Validate(zipBytes);
            await IndexedDb.PutString(IndexedDbContentStore.ZipKey, Convert.ToBase64String(zipBytes));
        }

        private static void Validate(byte[] zipBytes)
        {
            using ZipArchive zip = new(new MemoryStream(zipBytes), ZipArchiveMode.Read);
            if (zip.GetEntry(ContentManifest.FileName) is not { } manifestEntry)
            {
                throw new InvalidDataException("The asset bundle is missing its manifest.");
            }

            string manifestJson;
            using (Stream s = manifestEntry.Open())
            using (StreamReader r = new(s, Encoding.UTF8))
            {
                manifestJson = r.ReadToEnd();
            }

            IReadOnlyDictionary<string, string> manifest = ContentManifest.ParseFiles(manifestJson);
            IReadOnlyList<string> invalid = ContentManifest.FindInvalidFiles(manifest, rel =>
            {
                if (zip.GetEntry(rel) is not { } entry)
                {
                    return null;
                }
                using Stream s = entry.Open();
                using MemoryStream ms = new();
                s.CopyTo(ms);
                return ms.ToArray();
            });

            if (invalid.Count > 0)
            {
                throw new InvalidDataException(
                    $"The asset bundle is incomplete or corrupt. Invalid files: {ContentManifest.SummarizeInvalidFiles(invalid)}");
            }
        }
    }
}
