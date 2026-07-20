using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CtrDxEditor.Playtest
{
    /// <summary>Resolves a user-picked path to a runnable Cut the Rope: DX binary.</summary>
    /// <remarks>
    /// On macOS the user picks the <c>.app</c> bundle, which is a directory and cannot be executed
    /// directly. Resolution runs at launch time rather than at pick time so a stored path keeps
    /// working when the bundle is replaced in place by an update.
    /// </remarks>
    public static class DxExecutableResolver
    {
        /// <summary>Resolves <paramref name="pickedPath"/> to an executable file path.</summary>
        /// <param name="pickedPath">The path the user selected (a binary, or a macOS .app bundle).</param>
        /// <param name="executablePath">The resolved runnable path, or an empty string on failure.</param>
        /// <param name="error">The reason resolution failed, or null on success.</param>
        /// <returns>True when a runnable path was found.</returns>
        public static bool TryResolve(string pickedPath, out string executablePath, out string? error)
        {
            executablePath = "";

            if (string.IsNullOrWhiteSpace(pickedPath))
            {
                error = "No Cut the Rope: DX executable has been selected.";
                return false;
            }

            // A plain file is runnable as-is: the dev-mode binary, a Windows .exe, a Linux binary.
            if (!pickedPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                if (!File.Exists(pickedPath))
                {
                    error = $"The selected executable no longer exists:\n{pickedPath}";
                    return false;
                }
                executablePath = pickedPath;
                error = null;
                return true;
            }

            string macOsDir = Path.Combine(pickedPath, "Contents", "MacOS");
            if (!Directory.Exists(macOsDir))
            {
                error = $"The selected app bundle has no Contents/MacOS directory:\n{pickedPath}";
                return false;
            }

            // Preferred: whatever Info.plist declares. Then the bundle's own name, which is what the
            // shipped bundle uses. Both are checked for existence so a stale plist cannot win.
            string? declared = ReadBundleExecutableName(pickedPath);
            string bundleName = Path.GetFileNameWithoutExtension(pickedPath);

            foreach (string? candidate in new[] { declared, bundleName })
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }
                string full = Path.Combine(macOsDir, candidate);
                if (File.Exists(full))
                {
                    executablePath = full;
                    error = null;
                    return true;
                }
            }

            // Last resort: an unambiguous single binary (a renamed or repackaged build).
            string[] binaries = Directory.GetFiles(macOsDir);
            if (binaries.Length == 1)
            {
                executablePath = binaries[0];
                error = null;
                return true;
            }

            error = binaries.Length == 0
                ? $"The selected app bundle contains no executable:\n{pickedPath}"
                : $"Could not determine which executable to run in:\n{pickedPath}";
            return false;
        }

        // Returns CFBundleExecutable from Contents/Info.plist, or null when absent or unreadable.
        // A plist is <dict> with alternating <key>/<string> siblings, so the value is the first
        // <string> following the matching <key>. Any malformed or missing plist falls through to
        // the caller's name-based fallbacks rather than failing the launch.
        private static string? ReadBundleExecutableName(string bundlePath)
        {
            string plistPath = Path.Combine(bundlePath, "Contents", "Info.plist");
            if (!File.Exists(plistPath))
            {
                return null;
            }

            try
            {
                // Never resolve the plist's external DTD declaration: it would attempt a network
                // fetch on a path that must work offline.
                XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
                using XmlReader reader = XmlReader.Create(plistPath, settings);
                XDocument doc = XDocument.Load(reader);

                return doc.Descendants("key")
                    .FirstOrDefault(k => k.Value == "CFBundleExecutable")
                    ?.ElementsAfterSelf()
                    .FirstOrDefault(e => e.Name == "string")
                    ?.Value;
            }
            catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
