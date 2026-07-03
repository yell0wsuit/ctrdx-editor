using System.IO;
using System.IO.Compression;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for extracting downloaded content bundles.</summary>
    public class ContentDownloaderTests
    {
        /// <summary>Verifies that extracting a valid bundle produces a valid content directory.</summary>
        [Fact]
        public void ExtractIntoUnpacksAValidBundle()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-dl-").FullName;
            try
            {
                // Build a content tree and zip it (the bundle layout: files at the zip root).
                string src = Path.Combine(root, "src");
                _ = Directory.CreateDirectory(Path.Combine(src, "images"));
                File.WriteAllText(Path.Combine(src, "images", "a.png"), "x");
                File.WriteAllText(
                    Path.Combine(src, ContentManifest.FileName),
                                         /*lang=json,strict*/
                                         """{"files":{"images/a.png":"_"}}""");
                string zipPath = Path.Combine(root, "bundle.zip");
                ZipFile.CreateFromDirectory(src, zipPath);

                string dest = Path.Combine(root, "content");
                ContentDownloader.ExtractInto(zipPath, dest);

                Assert.True(ContentLocation.IsValid(dest));
            }
            finally { Directory.Delete(root, recursive: true); }
        }
    }
}
