using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

using CtrDxEditor.Content;

namespace CtrDxEditor.Browser.Content
{
    /// <summary>Content store backed by a zip bundle held as bytes in IndexedDB.</summary>
    public sealed class IndexedDbContentStore : IContentStore, IDisposable
    {
        /// <summary>IndexedDB key holding the content zip bytes.</summary>
        public const string ZipKey = "content.zip";

        private ZipArchive? _archive;

        private async Task<ZipArchive?> ArchiveAsync()
        {
            if (_archive is not null)
            {
                return _archive;
            }
            byte[] bytes = await IndexedDb.GetBytes(ZipKey);
            if (bytes.Length == 0)
            {
                return null;
            }
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
        public byte[] ReadBytes(string relPath)
        {
            using Stream s = Entry(LoadedArchive(), relPath).Open();
            using MemoryStream ms = new();
            s.CopyTo(ms);
            return ms.ToArray();
        }

        /// <inheritdoc />
        public string ReadText(string relPath)
        {
            using Stream s = Entry(LoadedArchive(), relPath).Open();
            using StreamReader r = new(s, Encoding.UTF8);
            return r.ReadToEnd();
        }

        /// <summary>
        /// The zip archive, which the async reads load into memory once during preload. Synchronous reads
        /// cannot themselves await the IndexedDB fetch, so they require it to already be resident; on the
        /// single-threaded browser it always is by the time on-demand sprites are requested.
        /// </summary>
        private ZipArchive LoadedArchive()
        {
            return _archive
                ?? throw new InvalidOperationException("Content archive not loaded; an async read must run first.");
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
