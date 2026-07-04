using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>Reads the content asset manifest (file_manifest.json) and detects missing or corrupt assets.</summary>
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

        /// <summary>
        /// Returns the manifest-listed relative paths that are missing or whose content hash does not match
        /// the manifest's recorded SHA-256, using <paramref name="tryReadBytes"/> to fetch each file's bytes
        /// (return null for a missing file). This is the install-time acceptance gate for a freshly downloaded
        /// or uploaded bundle - unlike <see cref="MissingFiles"/> it hashes every file, so it is too expensive
        /// to run on every app launch against an already-installed bundle.
        /// </summary>
        public static IReadOnlyList<string> FindInvalidFiles(
            IReadOnlyDictionary<string, string> manifest, Func<string, byte[]?> tryReadBytes)
        {
            List<string> invalid = [];
            foreach ((string rel, string expectedHash) in manifest)
            {
                byte[]? bytes = tryReadBytes(rel);
                if (bytes is null || !HashMatches(bytes, expectedHash))
                {
                    invalid.Add(rel);
                }
            }
            return invalid;
        }

        /// <summary>
        /// Async counterpart to <see cref="FindInvalidFiles"/> that hashes each file via the injected
        /// <paramref name="hashHexAsync"/> instead of managed SHA-256. Returns the manifest-listed relative
        /// paths that are missing (<paramref name="tryReadBytes"/> returns null) or whose lowercase-hex hash
        /// does not match the manifest's recorded value.
        /// </summary>
        public static async Task<IReadOnlyList<string>> FindInvalidFilesAsync(
            IReadOnlyDictionary<string, string> manifest,
            Func<string, byte[]?> tryReadBytes,
            Func<byte[], Task<string>> hashHexAsync)
        {
            List<string> invalid = [];
            foreach ((string rel, string expectedHash) in manifest)
            {
                byte[]? bytes = tryReadBytes(rel);
                if (bytes is null)
                {
                    invalid.Add(rel);
                    continue;
                }

                string actual = await hashHexAsync(bytes);
                if (!string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    invalid.Add(rel);
                }
            }
            return invalid;
        }

        /// <summary>Formats an invalid-files list as a bulleted, one-per-line list for a user-facing error message, capped to avoid an unwieldy wall of text.</summary>
        public static string SummarizeInvalidFiles(IReadOnlyList<string> invalid)
        {
            const int max = 5;
            string list = string.Join("\n", invalid.Take(max).Select(f => $"- {f}"));
            return invalid.Count > max ? $"{list}\n… and {invalid.Count - max} more" : list;
        }

        private static bool HashMatches(byte[] bytes, string expectedHash)
        {
            string actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
            return string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
