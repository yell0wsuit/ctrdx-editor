using System;
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

        /// <summary>The decoration fields (remember flag, rope skin, background) survive a JSON round-trip.</summary>
        [Fact]
        public void RoundTripsDecorationFields()
        {
            EditorSettings settings = new()
            {
                RememberDecoration = true,
                RopeSkin = 2,
                Background = 5,
            };
            string json = JsonSerializer.Serialize(settings, AppJsonContext.Default.EditorSettings);
            EditorSettings back = JsonSerializer.Deserialize(json, AppJsonContext.Default.EditorSettings)!;
            Assert.True(back.RememberDecoration);
            Assert.Equal(2, back.RopeSkin);
            Assert.Equal(5, back.Background);
        }

        /// <summary>Verifies the DX executable path survives a source-generated JSON round trip.</summary>
        [Fact]
        public void DxExecutablePathRoundTripsThroughJsonContext()
        {
            string json = JsonSerializer.Serialize(
                new EditorSettings { DxExecutablePath = "/Applications/CutTheRopeDX.app" },
                AppJsonContext.Default.EditorSettings);

            Assert.Contains("dxExecutablePath", json, StringComparison.Ordinal);

            EditorSettings? loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.EditorSettings);

            Assert.Equal("/Applications/CutTheRopeDX.app", loaded?.DxExecutablePath);
        }
    }
}
