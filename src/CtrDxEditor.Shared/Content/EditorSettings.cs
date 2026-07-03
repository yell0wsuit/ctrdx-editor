using System.Text.Json.Serialization;

namespace CtrDxEditor.Content
{
    /// <summary>Persisted editor settings.</summary>
    public sealed class EditorSettings
    {
        /// <summary>User-configured content directory path, if one has been saved.</summary>
        [JsonPropertyName("contentPath")]
        public string? ContentPath { get; set; }
    }
}
