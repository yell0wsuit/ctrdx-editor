using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for reading content manifests and detecting missing files.</summary>
    public class ContentManifestTests
    {
        /// <summary>Verifies that the files section is parsed into a relative-path hash map.</summary>
        [Fact]
        public void ReadParsesFilesSection()
        {
            string dir = Directory.CreateTempSubdirectory("ctrdx-manifest-").FullName;
            try
            {
                string path = Path.Combine(dir, ContentManifest.FileName);
                File.WriteAllText(path, /*lang=json,strict*/ """{"files":{"images/a.png":"abc","fonts/b.ttf":"def"}}""");

                IReadOnlyDictionary<string, string> manifest = ContentManifest.Read(path);

                Assert.Equal(2, manifest.Count);
                Assert.Equal("abc", manifest["images/a.png"]);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        /// <summary>Verifies that only absent manifest entries are reported missing.</summary>
        [Fact]
        public void MissingFilesListsOnlyAbsentEntries()
        {
            string dir = Directory.CreateTempSubdirectory("ctrdx-manifest-").FullName;
            try
            {
                _ = Directory.CreateDirectory(Path.Combine(dir, "images"));
                File.WriteAllText(Path.Combine(dir, "images", "present.png"), "x");
                Dictionary<string, string> manifest = new()
                {
                    ["images/present.png"] = "_",
                    ["images/absent.png"] = "_",
                };

                IReadOnlyList<string> missing = ContentManifest.MissingFiles(dir, manifest);

                _ = Assert.Single(missing);
                Assert.Equal("images/absent.png", missing[0]);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        /// <summary>Verifies FindInvalidFiles reports nothing when every file's content matches its recorded hash.</summary>
        [Fact]
        public void FindInvalidFilesReturnsEmptyWhenAllHashesMatch()
        {
            byte[] bytes = "PNGDATA"u8.ToArray();
            string realHash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            Dictionary<string, string> manifest = new() { ["images/a.png"] = realHash };

            IReadOnlyList<string> invalid = ContentManifest.FindInvalidFiles(manifest, _ => bytes);

            Assert.Empty(invalid);
        }

        /// <summary>Verifies FindInvalidFiles reports a file whose content hash does not match (corruption/tampering).</summary>
        [Fact]
        public void FindInvalidFilesReportsHashMismatch()
        {
            Dictionary<string, string> manifest = new() { ["images/a.png"] = "not-the-real-hash" };

            IReadOnlyList<string> invalid = ContentManifest.FindInvalidFiles(manifest, _ => "PNGDATA"u8.ToArray());

            _ = Assert.Single(invalid);
            Assert.Equal("images/a.png", invalid[0]);
        }

        /// <summary>Verifies FindInvalidFiles reports a manifest-listed file the reader can't find at all.</summary>
        [Fact]
        public void FindInvalidFilesReportsMissingFile()
        {
            Dictionary<string, string> manifest = new() { ["images/absent.png"] = "anything" };

            IReadOnlyList<string> invalid = ContentManifest.FindInvalidFiles(manifest, _ => null);

            _ = Assert.Single(invalid);
            Assert.Equal("images/absent.png", invalid[0]);
        }

        /// <summary>Verifies the async finder reports nothing when every file's injected hash matches.</summary>
        [Fact]
        public async Task FindInvalidFilesAsyncReturnsEmptyWhenAllHashesMatch()
        {
            byte[] bytes = "PNGDATA"u8.ToArray();
            string realHash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            Dictionary<string, string> manifest = new() { ["images/a.png"] = realHash };

            IReadOnlyList<string> invalid = await ContentManifest.FindInvalidFilesAsync(
                manifest, _ => bytes, b => Task.FromResult(Convert.ToHexStringLower(SHA256.HashData(b))));

            Assert.Empty(invalid);
        }

        /// <summary>Verifies the async finder reports a file whose injected hash does not match.</summary>
        [Fact]
        public async Task FindInvalidFilesAsyncReportsHashMismatch()
        {
            Dictionary<string, string> manifest = new() { ["images/a.png"] = "not-the-real-hash" };

            IReadOnlyList<string> invalid = await ContentManifest.FindInvalidFilesAsync(
                manifest, _ => "PNGDATA"u8.ToArray(),
                b => Task.FromResult(Convert.ToHexStringLower(SHA256.HashData(b))));

            _ = Assert.Single(invalid);
            Assert.Equal("images/a.png", invalid[0]);
        }

        /// <summary>Verifies the async finder reports a manifest-listed file the reader can't find at all.</summary>
        [Fact]
        public async Task FindInvalidFilesAsyncReportsMissingFile()
        {
            Dictionary<string, string> manifest = new() { ["images/absent.png"] = "anything" };

            IReadOnlyList<string> invalid = await ContentManifest.FindInvalidFilesAsync(
                manifest, _ => null, b => Task.FromResult("unused"));

            _ = Assert.Single(invalid);
            Assert.Equal("images/absent.png", invalid[0]);
        }

        /// <summary>Verifies the invalid-files summary renders as a bulleted, one-per-line list when under the cap.</summary>
        [Fact]
        public void SummarizeInvalidFilesListsAllWhenUnderCap()
        {
            string summary = ContentManifest.SummarizeInvalidFiles(["a.png", "b.png"]);

            Assert.Equal("- a.png\n- b.png", summary);
        }

        /// <summary>Verifies the invalid-files summary truncates with a count line once over the cap.</summary>
        [Fact]
        public void SummarizeInvalidFilesTruncatesOverCap()
        {
            string summary = ContentManifest.SummarizeInvalidFiles(["a", "b", "c", "d", "e", "f", "g"]);

            Assert.Equal("- a\n- b\n- c\n- d\n- e\n… and 2 more", summary);
        }
    }
}
