using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
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
            byte[] bytes = await http.GetByteArrayAsync(ContentDownloader.AssetsUrl, ct);
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
            if (zip.GetEntry(ContentManifest.FileName) is null)
            {
                throw new InvalidDataException("The asset bundle is missing its manifest.");
            }
        }
    }
}
