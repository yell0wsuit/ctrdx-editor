using System.IO;
using System.IO.Compression;
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
        // Real SHA-256 of the bytes WriteEntry actually writes for "PNGDATA": StreamWriter with
        // Encoding.UTF8 prepends a 3-byte BOM (EF BB BF), so this hashes BOM + "PNGDATA", not the
        // plain string - so the install-time hash verification accepts it.
        private const string PngDataHash = "f176147da6c819cc62adf2b557eeb4f8583073ffbfcf3417da73c3baa9af4d50";

        private static MemoryStream MakeContentZip(string manifestJson)
        {
            MemoryStream ms = new();
            using (ZipArchive zip = new(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteEntry(zip, "images/a.png", "PNGDATA");
                WriteEntry(zip, ContentManifest.FileName, manifestJson);
            }
            ms.Position = 0;
            return ms;
        }

        private static void WriteEntry(ZipArchive zip, string name, string content)
        {
            using StreamWriter w = new(zip.CreateEntry(name).Open(), Encoding.UTF8);
            w.Write(content);
        }

        /// <summary>Verifies that installing from a zip extracts valid content to the destination.</summary>
        [Fact]
        public async Task InstallFromZipExtractsValidContent()
        {
            string dest = Path.Combine(Directory.CreateTempSubdirectory("ctrdx-install-").FullName, "content");
            using MemoryStream zip = MakeContentZip(/*lang=json,strict*/ "{\"files\":{\"images/a.png\":\"HASH\"}}".Replace("HASH", PngDataHash));

            await new FolderContentInstaller(dest).InstallFromZipAsync(zip, CancellationToken.None);

            Assert.True(await new FolderContentStore(dest).IsPopulatedAsync());
        }

        /// <summary>Verifies that a zip whose content doesn't match its manifest's recorded hash is rejected.</summary>
        [Fact]
        public async Task InstallFromZipRejectsContentThatDoesNotMatchManifestHash()
        {
            string dest = Path.Combine(Directory.CreateTempSubdirectory("ctrdx-install-").FullName, "content");
            using MemoryStream zip = MakeContentZip(/*lang=json,strict*/ "{\"files\":{\"images/a.png\":\"not-the-real-hash\"}}");

            InvalidDataException ex = await Assert.ThrowsAsync<InvalidDataException>(
                () => new FolderContentInstaller(dest).InstallFromZipAsync(zip, CancellationToken.None));

            Assert.Contains("images/a.png", ex.Message);
        }
    }
}
