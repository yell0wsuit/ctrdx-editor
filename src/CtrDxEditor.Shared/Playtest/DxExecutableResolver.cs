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

            // A directory dragged from Finder arrives with a trailing separator, which would defeat
            // the .app suffix test below and send a bundle down the plain-folder path. Trimmed here,
            // once, so every check afterwards sees the same shape of path. A path that is nothing but
            // separators is left alone rather than trimmed away to nothing.
            string trimmed = pickedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (trimmed.Length > 0)
            {
                pickedPath = trimmed;
            }

            if (!pickedPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                // A plain file is runnable as-is: the dev-mode binary, a Windows .exe, a Linux binary.
                if (File.Exists(pickedPath))
                {
                    executablePath = pickedPath;
                    error = null;
                    return true;
                }

                // A plain directory is what macOS users can actually select: neither picker will
                // return a .app (the file picker cannot express a directory, and the folder picker
                // treats a bundle as an unselectable package), so they choose its parent folder.
                if (Directory.Exists(pickedPath))
                {
                    return TryResolveFromContainer(pickedPath, out executablePath, out error);
                }

                error = $"The selected executable no longer exists:\n{pickedPath}";
                return false;
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

        // Resolves through a bundle sitting directly inside a dropped folder. Only an unambiguous
        // single bundle is accepted: guessing between several would risk launching the wrong app,
        // and the user can always drag the bundle itself instead.
        private static bool TryResolveFromContainer(string directory, out string executablePath, out string? error)
        {
            executablePath = "";

            string[] bundles;
            try
            {
                bundles = Directory.GetDirectories(directory, "*.app");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                error = $"The selected folder could not be read:\n{directory}";
                return false;
            }

            if (bundles.Length == 1)
            {
                return TryResolve(bundles[0], out executablePath, out error);
            }

            error = bundles.Length == 0
                ? $"No application was found in:\n{directory}\n\nDrag CutTheRope-DX.app itself instead."
                : $"That folder contains several applications:\n{directory}\n\nDrag CutTheRope-DX.app itself instead.";
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
