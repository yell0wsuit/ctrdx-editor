using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CtrDxEditor.Content
{
    /// <summary>Persisted editor settings, stored as JSON next to the executable.</summary>
    public sealed class EditorSettings
    {
        [JsonPropertyName("contentPath")]
        public string? ContentPath { get; set; }

        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        /// <summary>Loads settings from <paramref name="path"/>; returns empty settings when the file is missing or unreadable.</summary>
        public static EditorSettings Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return new EditorSettings();
                }
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<EditorSettings>(json, Options) ?? new EditorSettings();
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                return new EditorSettings();
            }
        }

        /// <summary>Writes settings to <paramref name="path"/>, creating the parent directory when needed.</summary>
        public void Save(string path)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, JsonSerializer.Serialize(this, Options));
        }
    }
}
