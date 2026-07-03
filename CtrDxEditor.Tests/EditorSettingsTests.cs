using System;
using System.IO;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for loading and saving persisted editor settings.</summary>
    public class EditorSettingsTests
    {
        /// <summary>Verifies that saving then loading preserves the configured content path.</summary>
        [Fact]
        public void SaveThenLoadRoundTripsContentPath()
        {
            string dir = Directory.CreateTempSubdirectory("ctrdx-settings-").FullName;
            try
            {
                string path = Path.Combine(dir, "EditorConfig", "settings.json");
                new EditorSettings { ContentPath = "/some/content" }.Save(path);

                EditorSettings loaded = EditorSettings.Load(path);

                Assert.Equal("/some/content", loaded.ContentPath);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        /// <summary>Verifies that missing settings files load as empty settings.</summary>
        [Fact]
        public void LoadMissingFileReturnsEmptySettings()
        {
            string path = Path.Combine(
                Path.GetTempPath(), "ctrdx-missing-" + Guid.NewGuid().ToString("N"), "settings.json");

            EditorSettings loaded = EditorSettings.Load(path);

            Assert.Null(loaded.ContentPath);
        }
    }
}
