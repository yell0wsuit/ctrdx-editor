using System.Collections.Generic;
using System.IO;

namespace CtrDxEditor.Content
{
    /// <summary>Pure content-directory resolution: validity checks and ordered lookup.</summary>
    public static class ContentLocation
    {
        /// <summary>A directory is valid content when it holds the manifest and every file the manifest lists.</summary>
        public static bool IsValid(string? contentDir)
        {
            if (string.IsNullOrEmpty(contentDir))
            {
                return false;
            }
            string manifestPath = Path.Combine(contentDir, ContentManifest.FileName);
            if (!File.Exists(manifestPath))
            {
                return false;
            }
            IReadOnlyDictionary<string, string> manifest = ContentManifest.Read(manifestPath);
            return ContentManifest.MissingFiles(contentDir, manifest).Count == 0;
        }

        /// <summary>
        /// Resolves the content directory, first valid wins: the configured path, then a "content"
        /// folder next to <paramref name="baseDir"/>, then in any ancestor of it. Returns null when
        /// none are valid.
        /// </summary>
        public static string? Resolve(string baseDir, string? configuredPath)
        {
            if (IsValid(configuredPath))
            {
                return configuredPath;
            }

            for (DirectoryInfo? dir = new(baseDir); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "content");
                if (IsValid(candidate))
                {
                    return candidate;
                }
            }
            return null;
        }
    }
}
