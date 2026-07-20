using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace CtrDxEditor.Playtest
{
    /// <summary>Owns the temp directory holding the level file handed to Cut the Rope: DX.</summary>
    /// <remarks>
    /// One directory per editor session, holding a single level file at a constant path. The path is
    /// constant on purpose: the game binds a file watcher to whatever path it was launched with and
    /// reloads on change, so rewriting that exact file is how an edit reaches a running game.
    /// The directory is removed on disposal; directories left behind by a crashed editor are cleared
    /// by <see cref="SweepStale"/> at startup.
    /// </remarks>
    public sealed class PlaytestTempStore : IDisposable
    {
        private const string RootFolderName = "CtrDxEditor";
        private const string SessionPrefix = "playtest-";
        private const string LevelFileName = "level.xml";

        private bool _disposed;

        /// <summary>Creates a store for this editor session.</summary>
        /// <param name="rootOverride">Container for session directories; defaults to the system temp root. Tests pass their own.</param>
        public PlaytestTempStore(string? rootOverride = null)
        {
            string root = rootOverride ?? DefaultRoot();
            string unique = Guid.NewGuid().ToString("N")[..8];
            SessionDirectory = Path.Combine(root, $"{SessionPrefix}{Environment.ProcessId}-{unique}");
        }

        /// <summary>The directory holding this session's level file. Created on first write.</summary>
        public string SessionDirectory { get; }

        /// <summary>The constant path of this session's level file, valid before it exists.</summary>
        public string LevelPath => Path.Combine(SessionDirectory, LevelFileName);

        /// <summary>Replaces the level file with <paramref name="levelXml"/>.</summary>
        /// <param name="levelXml">Serialized level XML to hand to the game.</param>
        /// <returns><see cref="LevelPath"/>, unchanged across calls.</returns>
        /// <remarks>
        /// Written to a sibling scratch file and moved into place, so a game watching the level never
        /// observes a partially-written document. Same-directory moves are atomic.
        /// </remarks>
        public string Write(string levelXml)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _ = Directory.CreateDirectory(SessionDirectory);
            string target = LevelPath;
            string scratch = target + ".tmp";
            File.WriteAllText(scratch, levelXml);
            File.Move(scratch, target, overwrite: true);
            return target;
        }

        /// <summary>Deletes this session's directory. Never throws.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            TryDeleteDirectory(SessionDirectory);
        }

        /// <summary>Deletes session directories whose owning editor process is no longer running.</summary>
        /// <param name="rootOverride">Container for session directories; defaults to the system temp root. Tests pass their own.</param>
        /// <param name="isProcessAlive">Liveness probe for a process id; defaults to a real process lookup. Tests substitute their own.</param>
        /// <remarks>
        /// Leftover playtest XML has no value once its editor session ends, so age is not a useful
        /// criterion. Liveness is: a second editor running concurrently owns a live session directory,
        /// and deleting it would break that editor's playtest.
        /// </remarks>
        public static void SweepStale(string? rootOverride = null, Func<int, bool>? isProcessAlive = null)
        {
            string root = rootOverride ?? DefaultRoot();
            if (!Directory.Exists(root))
            {
                return;
            }

            Func<int, bool> alive = isProcessAlive ?? IsProcessAlive;

            string[] candidates;
            try
            {
                candidates = Directory.GetDirectories(root, SessionPrefix + "*");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return;
            }

            foreach (string dir in candidates)
            {
                if (TryReadOwnerPid(Path.GetFileName(dir)) is not { } pid)
                {
                    continue; // Not a name this type produced; leave it alone.
                }
                if (!alive(pid))
                {
                    TryDeleteDirectory(dir);
                }
            }
        }

        private static string DefaultRoot()
        {
            return Path.Combine(Path.GetTempPath(), RootFolderName);
        }

        // "playtest-<pid>-<hex>" -> pid. Returns null for any name that does not match, so unrelated
        // directories under the root are never touched.
        private static int? TryReadOwnerPid(string directoryName)
        {
            if (!directoryName.StartsWith(SessionPrefix, StringComparison.Ordinal))
            {
                return null;
            }
            string rest = directoryName[SessionPrefix.Length..];
            int dash = rest.IndexOf('-', StringComparison.Ordinal);
            return dash > 0
                && int.TryParse(rest[..dash], NumberStyles.None, CultureInfo.InvariantCulture, out int pid)
                ? pid
                : null;
        }

        // A pid with no live process throws; treat any failure as "not running". Pid reuse can spare a
        // stale directory, which is harmless: it holds one small file and a later startup sweeps it.
        private static bool IsProcessAlive(int pid)
        {
            try
            {
                using Process process = Process.GetProcessById(pid);
                return !process.HasExited;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
            {
                return false;
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best effort: a failed temp cleanup must never block shutdown.
            }
        }
    }
}
