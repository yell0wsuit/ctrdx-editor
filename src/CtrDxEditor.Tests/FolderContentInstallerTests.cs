using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for the folder-backed content installer.</summary>
    public class FolderContentInstallerTests
    {
        // Builds an in-memory zip containing every sprite file the given platform needs, plus a
        // correctly-hashed file_manifest.json, so it passes install validation for that platform.
        // tamperHashFor optionally corrupts one file's recorded hash to simulate corruption.
        private static MemoryStream MakeBundle(string imageExtension, string? tamperHashFor = null)
        {
            IReadOnlyCollection<string> required = VisualDescriptorMap.RequiredFiles(imageExtension);
            Dictionary<string, string> hashes = [];
            MemoryStream ms = new();
            using (ZipArchive zip = new(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (string rel in required)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes($"data:{rel}");
                    using (Stream s = zip.CreateEntry(rel).Open())
                    {
                        s.Write(bytes);
                    }
                    hashes[rel] = rel == tamperHashFor
                        ? new string('0', 64)
                        : Convert.ToHexStringLower(SHA256.HashData(bytes));
                }
                string filesJson = string.Join(",", hashes.Select(kv => $"\"{kv.Key}\":\"{kv.Value}\""));
                using (Stream s = zip.CreateEntry(ContentManifest.FileName).Open())
                {
                    s.Write(Encoding.UTF8.GetBytes($"{{\"files\":{{{filesJson}}}}}"));
                }
            }
            ms.Position = 0;
            return ms;
        }

        private static string DestDir()
        {
            return Path.Combine(Directory.CreateTempSubdirectory("ctrdx-install-").FullName, "content");
        }

        /// <summary>Verifies that installing a valid platform bundle extracts populated content to the destination.</summary>
        [Fact]
        public async Task InstallFromZipExtractsValidContent()
        {
            string dest = DestDir();
            using MemoryStream zip = MakeBundle(".png");

            await new FolderContentInstaller(dest, ".png").InstallFromZipAsync(zip, CancellationToken.None);

            Assert.True(await new FolderContentStore(dest).IsPopulatedAsync());
        }

        /// <summary>Verifies that a zip whose content doesn't match its manifest hash is rejected and leaves no content behind.</summary>
        [Fact]
        public async Task InstallFromZipRejectsContentThatDoesNotMatchManifestHash()
        {
            string dest = DestDir();
            using MemoryStream zip = MakeBundle(".png", tamperHashFor: "images/obj_hook.png");

            InvalidDataException ex = await Assert.ThrowsAsync<InvalidDataException>(
                () => new FolderContentInstaller(dest, ".png").InstallFromZipAsync(zip, CancellationToken.None));

            Assert.Contains("images/obj_hook.png", ex.Message);
            Assert.False(Directory.Exists(dest)); // atomic install: a rejected bundle never lands
        }

        /// <summary>Verifies that a bundle built for another platform (WebP under a PNG head) is rejected up front.</summary>
        [Fact]
        public async Task InstallFromZipRejectsBundleBuiltForAnotherPlatform()
        {
            string dest = DestDir();
            using MemoryStream zip = MakeBundle(".webp"); // a browser (WebP) bundle...

            InvalidDataException ex = await Assert.ThrowsAsync<InvalidDataException>(
                () => new FolderContentInstaller(dest, ".png").InstallFromZipAsync(zip, CancellationToken.None));

            // ...installed under the desktop (.png) head: its required PNG atlases are absent.
            Assert.Contains(".png", ex.Message);
            Assert.False(Directory.Exists(dest));
        }
    }
}
