using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CtrDxEditor.Content
{
    /// <summary>Reads the content asset manifest (file_manifest.json) and detects missing assets.</summary>
    public static class ContentManifest
    {
        /// <summary>The content manifest filename expected inside a content directory.</summary>
        public const string FileName = "file_manifest.json";

        /// <summary>Parses a manifest JSON string's { "files": { "rel/path": "sha256", ... } } section.</summary>
        public static IReadOnlyDictionary<string, string> ParseFiles(string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            Dictionary<string, string> result = [];
            if (doc.RootElement.TryGetProperty("files", out JsonElement files))
            {
                foreach (JsonProperty entry in files.EnumerateObject())
                {
                    result[entry.Name] = entry.Value.GetString() ?? "";
                }
            }
            return result;
        }

        /// <summary>Parses the manifest's { "files": { "relative/path": "sha256", ... } } section into a rel-path -> hash map.</summary>
        public static IReadOnlyDictionary<string, string> Read(string manifestPath)
        {
            return ParseFiles(File.ReadAllText(manifestPath));
        }

        /// <summary>Returns the manifest-listed files (relative POSIX paths) absent from <paramref name="contentDir"/>.</summary>
        public static IReadOnlyList<string> MissingFiles(
            string contentDir, IReadOnlyDictionary<string, string> manifest)
        {
            List<string> missing = [];
            foreach (string rel in manifest.Keys)
            {
                string local = Path.Combine(contentDir, rel.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(local))
                {
                    missing.Add(rel);
                }
            }
            return missing;
        }
    }
}
