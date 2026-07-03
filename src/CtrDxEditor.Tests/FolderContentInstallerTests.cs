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
        private static MemoryStream MakeContentZip()
        {
            MemoryStream ms = new();
            using (ZipArchive zip = new(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteEntry(zip, "images/a.png", "PNGDATA");
                WriteEntry(zip, ContentManifest.FileName, /*lang=json,strict*/ """{"files":{"images/a.png":"_"}}""");
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
            using MemoryStream zip = MakeContentZip();

            await new FolderContentInstaller(dest).InstallFromZipAsync(zip, CancellationToken.None);

            Assert.True(await new FolderContentStore(dest).IsPopulatedAsync());
        }
    }
}
