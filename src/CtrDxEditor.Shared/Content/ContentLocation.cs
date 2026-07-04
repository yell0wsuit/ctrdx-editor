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
        /// Install-time acceptance gate for freshly extracted content. Verifies every manifest-listed
        /// file exists and matches its recorded SHA-256 hash, and that the sprite atlases this platform
        /// renders are present for <paramref name="imageExtension"/> (so a bundle built for the wrong
        /// platform - e.g. WebP content under a PNG head - is rejected instead of crashing at load).
        /// Returns the invalid (missing/corrupt/wrong-platform) relative paths; empty when fully valid.
        /// Hashes every file, so it is a one-time install check, not something to run on every launch.
        /// </summary>
        public static IReadOnlyList<string> FindInvalidFiles(string contentDir, string imageExtension)
        {
            string manifestPath = Path.Combine(contentDir, ContentManifest.FileName);
            if (!File.Exists(manifestPath))
            {
                return [ContentManifest.FileName];
            }

            bool Exists(string rel)
            {
                return File.Exists(Path.Combine(contentDir, rel.Replace('/', Path.DirectorySeparatorChar)));
            }

            IReadOnlyDictionary<string, string> manifest = ContentManifest.Read(manifestPath);
            List<string> invalid =
            [
                .. ContentManifest.FindInvalidFiles(manifest, rel =>
                {
                    string local = Path.Combine(contentDir, rel.Replace('/', Path.DirectorySeparatorChar));
                    return File.Exists(local) ? File.ReadAllBytes(local) : null;
                }),
            ];

            // Platform-specific sprite atlases the app will actually load; absence here means the bundle
            // is for the wrong head, even when its own manifest is internally consistent.
            foreach (string rel in VisualDescriptorMap.RequiredFiles(imageExtension))
            {
                if (!Exists(rel) && !invalid.Contains(rel))
                {
                    invalid.Add(rel);
                }
            }
            return invalid;
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
