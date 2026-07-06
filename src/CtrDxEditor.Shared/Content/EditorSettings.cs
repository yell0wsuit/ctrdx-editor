using System.Text.Json.Serialization;

namespace CtrDxEditor.Content
{
    /// <summary>Persisted editor settings.</summary>
    public sealed class EditorSettings
    {
        /// <summary>User-configured content directory path, if one has been saved.</summary>
        [JsonPropertyName("contentPath")]
        public string? ContentPath { get; set; }

        /// <summary>Whether the New Level dialog persists its decoration picks as defaults.</summary>
        [JsonPropertyName("rememberDecoration")]
        public bool RememberDecoration { get; set; }

        /// <summary>Remembered rope skin id: -1 = Random, else 0..8 (0 = default brown).</summary>
        [JsonPropertyName("ropeSkin")]
        public int RopeSkin { get; set; }

        /// <summary>Remembered background id: 0 = Blank, 1..7 = bgr_01..bgr_07, -1 = Random.</summary>
        [JsonPropertyName("background")]
        public int Background { get; set; }
    }
}
