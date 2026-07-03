using System.Collections.Generic;
using System.Text.Json;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for persisted editor settings JSON metadata.</summary>
    public class EditorSettingsTests
    {
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
