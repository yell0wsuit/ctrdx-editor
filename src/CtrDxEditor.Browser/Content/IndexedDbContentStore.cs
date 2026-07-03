using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

using CtrDxEditor.Content;

namespace CtrDxEditor.Browser.Content
{
    /// <summary>Content store backed by a zip bundle held as a base64 blob in IndexedDB.</summary>
    public sealed class IndexedDbContentStore : IContentStore, IDisposable
    {
        /// <summary>IndexedDB key holding the content zip as base64.</summary>
        public const string ZipKey = "content.zip";

        private ZipArchive? _archive;

        private async Task<ZipArchive?> ArchiveAsync()
        {
            if (_archive is not null)
            {
                return _archive;
            }
            string? b64 = await IndexedDb.GetString(ZipKey);
            if (string.IsNullOrEmpty(b64))
            {
                return null;
            }
            byte[] bytes = Convert.FromBase64String(b64);
            _archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
            return _archive;
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string relPath)
        {
            return (await ArchiveAsync())?.GetEntry(relPath) is not null;
        }

        /// <inheritdoc />
        public async Task<byte[]> ReadBytesAsync(string relPath)
        {
            ZipArchiveEntry entry = Entry(await ArchiveAsync(), relPath);
            using Stream s = entry.Open();
            using MemoryStream ms = new();
            await s.CopyToAsync(ms);
            return ms.ToArray();
        }

        /// <inheritdoc />
        public async Task<string> ReadTextAsync(string relPath)
        {
            ZipArchiveEntry entry = Entry(await ArchiveAsync(), relPath);
            using Stream s = entry.Open();
            using StreamReader r = new(s, Encoding.UTF8);
            return await r.ReadToEndAsync();
        }

        /// <inheritdoc />
        public async Task<bool> IsPopulatedAsync()
        {
            ZipArchive? zip = await ArchiveAsync();
            if (zip?.GetEntry(ContentManifest.FileName) is not { } manifestEntry)
            {
                return false;
            }
            using Stream ms = manifestEntry.Open();
            using StreamReader r = new(ms, Encoding.UTF8);
            IReadOnlyDictionary<string, string> manifest = ContentManifest.ParseFiles(await r.ReadToEndAsync());
            foreach (string rel in manifest.Keys)
            {
                if (zip.GetEntry(rel) is null)
                {
                    return false;
                }
            }
            return true;
        }

        private static ZipArchiveEntry Entry(ZipArchive? zip, string relPath)
        {
            return zip?.GetEntry(relPath) ?? throw new FileNotFoundException($"Content entry not found: {relPath}");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _archive?.Dispose();
        }
    }
}
