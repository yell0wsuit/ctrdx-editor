using System.Text.Json.Serialization;

namespace CtrDxEditor.Content
{
    /// <summary>Persisted editor settings.</summary>
    public sealed class EditorSettings
    {
        /// <summary>User-configured content directory path, if one has been saved.</summary>
        [JsonPropertyName("contentPath")]
        public string? ContentPath { get; set; }

        /// <summary>Path to the Cut the Rope: DX executable used for playtesting, as picked by the user
        /// (on macOS this is the .app bundle itself, resolved to its inner binary at launch time).</summary>
        [JsonPropertyName("dxExecutablePath")]
        public string? DxExecutablePath { get; set; }

        /// <summary>Whether the New Level dialog persists its decoration picks as defaults.</summary>
        [JsonPropertyName("rememberDecoration")]
        public bool RememberDecoration { get; set; }

        /// <summary>Remembered rope skin id: -1 = Random, else 0..8 (0 = default brown).</summary>
        [JsonPropertyName("ropeSkin")]
        public int RopeSkin { get; set; }

        /// <summary>Remembered background id: 0 = Blank, 1..7 = bgr_01..bgr_07, -1 = Random.</summary>
        [JsonPropertyName("background")]
        public int Background { get; set; }

        /// <summary>Remembered candy skin index: 0 = Candy 1 (default), 1..51 = Candy 2..52, -1 = Random.</summary>
        [JsonPropertyName("candySkin")]
        public int CandySkin { get; set; }

        /// <summary>Remembered Om Nom sitting platform: 0 = Platform 1 (default), 1..16 = Platform 2..17, -1 = Random.</summary>
        [JsonPropertyName("omNomSupport")]
        public int OmNomSupport { get; set; }
    }
}
