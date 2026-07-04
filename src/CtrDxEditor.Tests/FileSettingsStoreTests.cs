using System;
using System.IO;
using System.Threading.Tasks;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for the file-backed settings store.</summary>
    public class FileSettingsStoreTests
    {
        /// <summary>Verifies that saved settings can be loaded from the same store.</summary>
        [Fact]
        public async Task SaveThenLoadRoundTripsContentPath()
        {
            string dir = Directory.CreateTempSubdirectory("ctrdx-settings-").FullName;
            try
            {
                string path = Path.Combine(dir, "EditorConfig", "settings.json");
                FileSettingsStore store = new(path);

                await store.SaveAsync(new EditorSettings { ContentPath = "/some/content" });
                EditorSettings loaded = await store.LoadAsync();

                Assert.Equal("/some/content", loaded.ContentPath);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        /// <summary>Verifies that missing settings files load as empty settings.</summary>
        [Fact]
        public async Task LoadMissingFileReturnsEmptySettings()
        {
            string path = Path.Combine(
                Path.GetTempPath(), $"ctrdx-missing-{Guid.NewGuid():N}", "settings.json");

            EditorSettings loaded = await new FileSettingsStore(path).LoadAsync();

            Assert.Null(loaded.ContentPath);
        }
    }
}
