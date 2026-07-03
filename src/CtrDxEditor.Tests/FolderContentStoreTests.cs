using System.IO;
using System.Threading.Tasks;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for the folder-backed content store.</summary>
    public class FolderContentStoreTests
    {
        private static string MakePopulated(string root)
        {
            _ = Directory.CreateDirectory(Path.Combine(root, "images"));
            File.WriteAllText(Path.Combine(root, "images", "a.png"), "PNGDATA");
            File.WriteAllText(
                Path.Combine(root, ContentManifest.FileName),
                /*lang=json,strict*/ """{"files":{"images/a.png":"_"}}""");
            return root;
        }

        /// <summary>Verifies that existence and read APIs resolve manifest-relative paths.</summary>
        [Fact]
        public async Task ExistsAndReadReturnContentByRelPath()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-store-").FullName;
            try
            {
                _ = MakePopulated(root);
                FolderContentStore store = new(root);

                Assert.True(await store.ExistsAsync("images/a.png"));
                Assert.False(await store.ExistsAsync("images/missing.png"));
                Assert.Equal("PNGDATA", await store.ReadTextAsync("images/a.png"));
                Assert.Equal(7, (await store.ReadBytesAsync("images/a.png")).Length);
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies that population requires both the manifest and every listed file.</summary>
        [Fact]
        public async Task IsPopulatedTrueOnlyWhenManifestAndFilesPresent()
        {
            string full = Directory.CreateTempSubdirectory("ctrdx-store-").FullName;
            string empty = Directory.CreateTempSubdirectory("ctrdx-store-").FullName;
            try
            {
                _ = MakePopulated(full);
                Assert.True(await new FolderContentStore(full).IsPopulatedAsync());
                Assert.False(await new FolderContentStore(empty).IsPopulatedAsync());
            }
            finally { Directory.Delete(full, recursive: true); Directory.Delete(empty, recursive: true); }
        }
    }
}
