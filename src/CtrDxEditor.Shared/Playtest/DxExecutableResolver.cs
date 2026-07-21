using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using CtrDxEditor.Localization;

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
                error = Localizer.Get("Playtest.Resolve.NothingSelected");
                return false;
            }

            pickedPath = TrimTrailingSeparators(pickedPath);

            if (!pickedPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                // A plain file is runnable as-is: the dev-mode binary, a Windows .exe, a Linux binary
                // or AppImage. Only the execute bit stands between it and a launch.
                if (File.Exists(pickedPath))
                {
                    if (!IsExecutable(pickedPath))
                    {
                        executablePath = "";
                        error = Message("Playtest.Resolve.NotExecutable", pickedPath);
                        return false;
                    }

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

                error = Message("Playtest.Resolve.MissingFile", pickedPath);
                return false;
            }

            string macOsDir = Path.Combine(pickedPath, "Contents", "MacOS");
            if (!Directory.Exists(macOsDir))
            {
                error = Message("Playtest.Resolve.BundleNoMacOsDir", pickedPath);
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

            error = Message(
                binaries.Length == 0 ? "Playtest.Resolve.BundleEmpty" : "Playtest.Resolve.BundleAmbiguous",
                pickedPath);
            return false;
        }

        /// <summary>Whether a path is a macOS app bundle, or a folder holding at least one.</summary>
        /// <remarks>
        /// Only the macOS drop route uses this. Dropping is where a stray document is easiest to send in
        /// by accident, and the drop hint there asks for a bundle by name, so the drop is held to it.
        /// Resolution itself stays permissive - a typed dev-build binary has no .app around it and must
        /// keep working - which is why this is a separate check rather than a rule inside
        /// <see cref="TryResolve"/>.
        /// </remarks>
        /// <param name="path">The dropped path.</param>
        /// <returns>True when the path is or contains an app bundle.</returns>
        public static bool IsBundleOrBundleContainer(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            path = TrimTrailingSeparators(path);
            if (path.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                return Directory.Exists(path) && Directory.GetDirectories(path, "*.app").Length > 0;
            }
            // An unreadable folder cannot be shown to hold a bundle, and the drop is refused with the
            // same wording as any other non-bundle rather than failing the dialog outright.
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        // Localized message with the offending path substituted in. Replace rather than string.Format:
        // a path is arbitrary user text that may contain braces, which a format call would choke on,
        // and several of these strings use {0} more than once.
        private static string Message(string key, string path)
        {
            return Localizer.Get(key).Replace("{0}", path, StringComparison.Ordinal);
        }

        // Whether a file carries an execute bit. An AppImage downloaded from a release page has none
        // until the user chmods it, and Process.Start would surface that as a bare "Permission denied"
        // with no hint of the fix. Only the plain-file route is checked: a binary inside a .app got its
        // bits from the packaging, and any bundle that lost them has bigger problems than this message.
        // Windows has no execute bit, so every file passes there.
        private static bool IsExecutable(string path)
        {
            if (OperatingSystem.IsWindows())
            {
                return true;
            }

            try
            {
                UnixFileMode mode = File.GetUnixFileMode(path);
                return (mode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
            }
            // An unreadable mode is not evidence of a missing bit, so the launch is allowed to proceed
            // and fail with the OS's own error rather than being blocked by a guess.
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                return true;
            }
        }

        // A directory dragged from Finder arrives with a trailing separator, which would defeat the .app
        // suffix tests and send a bundle down the plain-folder path. Trimmed once, up front, so every
        // check afterwards sees the same shape of path. A path that is nothing but separators is left
        // alone rather than trimmed away to nothing.
        private static string TrimTrailingSeparators(string path)
        {
            string trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return trimmed.Length > 0 ? trimmed : path;
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
                error = Message("Playtest.Resolve.FolderUnreadable", directory);
                return false;
            }

            if (bundles.Length == 1)
            {
                return TryResolve(bundles[0], out executablePath, out error);
            }

            error = Message(
                bundles.Length == 0 ? "Playtest.Resolve.FolderNoBundle" : "Playtest.Resolve.FolderManyBundles",
                directory);
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
