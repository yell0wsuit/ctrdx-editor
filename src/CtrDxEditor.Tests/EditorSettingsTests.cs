using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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

        /// <summary>Verifies NativeAOT-safe JSON metadata is generated for app JSON payloads.</summary>
        [Fact]
        public void AppJsonContextProvidesMetadataForSettingsAndLocalization()
        {
            string json = JsonSerializer.Serialize(
                new EditorSettings { ContentPath = "/aot/content" },
                AppJsonContext.Default.EditorSettings);

            EditorSettings? loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.EditorSettings);

            Assert.Equal("/aot/content", loaded?.ContentPath);
            Dictionary<string, string>? strings = JsonSerializer.Deserialize(
                """{"Window.Title":"Editor"}""",
                AppJsonContext.Default.DictionaryStringString);

            Assert.Equal("Editor", Assert.IsType<Dictionary<string, string>>(strings)["Window.Title"]);
        }
    }
}
