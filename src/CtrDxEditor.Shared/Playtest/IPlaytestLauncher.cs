using System;

namespace CtrDxEditor.Playtest
{
    /// <summary>Describes how a playtest process ended.</summary>
    /// <param name="exitCode">The game's process exit code.</param>
    /// <param name="standardError">Whatever the game wrote to stderr, possibly empty.</param>
    public sealed class PlaytestExitedEventArgs(int exitCode, string standardError) : EventArgs
    {
        /// <summary>The game's process exit code; non-zero means it refused the level or crashed.</summary>
        public int ExitCode { get; } = exitCode;

        /// <summary>Whatever the game wrote to stderr, possibly empty.</summary>
        public string StandardError { get; } = standardError;
    }

    /// <summary>Describes a launched program that never identified itself as a compatible Cut the Rope: DX.</summary>
    /// <param name="executablePath">The resolved executable that was launched but produced no handshake.</param>
    public sealed class PlaytestUnsupportedEventArgs(string executablePath) : EventArgs
    {
        /// <summary>The resolved executable that was launched but produced no handshake.</summary>
        public string ExecutablePath { get; } = executablePath;
    }

    /// <summary>Describes a level a running game refused to load.</summary>
    /// <param name="message">The game's own description of what was wrong with the level.</param>
    public sealed class PlaytestLevelRejectedEventArgs(string message) : EventArgs
    {
        /// <summary>The game's own description of what was wrong with the level.</summary>
        public string Message { get; } = message;
    }

    /// <summary>
    /// Implemented by launchers whose launch can be refused by the environment rather than failing.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="IPlaytestLauncher"/> because only the browser can experience it: a
    /// pop-up blocker rejects <c>window.open</c> when the user gesture that authorized it has been
    /// spent on an intervening dialog. Desktop has no equivalent and does not implement this.
    /// </remarks>
    public interface IBlockableLauncher
    {
        /// <summary>Whether the most recent launch attempt was refused by the environment.</summary>
        bool LastLaunchBlocked { get; }
    }

    /// <summary>Plays a level in Cut the Rope: DX. Absent on platforms that cannot spawn processes.</summary>
    public interface IPlaytestLauncher : IDisposable
    {
        /// <summary>Raised when the playtest process exits. Not raised on the UI thread.</summary>
        event EventHandler<PlaytestExitedEventArgs>? Exited;

        /// <summary>
        /// Raised when a freshly launched process produced no Cut the Rope: DX handshake within the grace
        /// period - it is a different program, or a build too old to understand <c>--level</c>. Not raised
        /// on the UI thread, and never raised for a reload (<see cref="Play"/> returning false).
        /// </summary>
        event EventHandler<PlaytestUnsupportedEventArgs>? Unsupported;

        /// <summary>
        /// Whether <see cref="Play"/> needs a user-picked executable. False on heads that always know
        /// where the game is - the browser, which opens a URL rather than a program.
        /// </summary>
        /// <remarks>
        /// Callers use this to decide whether to gate Play on a stored location and whether to offer a
        /// "set location" command at all. A head that answers false must accept null for
        /// <c>executablePath</c>.
        /// </remarks>
        bool RequiresLocation { get; }

        /// <summary>
        /// Raised when a running game reports a level it could not load. Not raised on the UI thread.
        /// </summary>
        /// <remarks>
        /// Deliberately separate from <see cref="Exited"/>: the game has not exited, it is still
        /// running the level it had before, so reporting this as an exit would misdescribe it. Only
        /// heads whose transport carries diagnostics back raise this; desktop reports the equivalent
        /// through <see cref="Exited"/>'s standard error.
        /// </remarks>
        event EventHandler<PlaytestLevelRejectedEventArgs>? LevelRejected;

        /// <summary>Writes <paramref name="levelXml"/> to the session level file, starting the game if it is not already running.</summary>
        /// <param name="executablePath">
        /// The user-picked executable or macOS .app bundle path; null on heads where
        /// <see cref="RequiresLocation"/> is false.
        /// </param>
        /// <param name="levelXml">Serialized level XML to play.</param>
        /// <returns>
        /// True when a new game process was started; false when a already-running game was handed the
        /// level instead. Callers use this to distinguish a cold launch, which is worth announcing,
        /// from a reload, which the game reports itself.
        /// </returns>
        /// <remarks>
        /// When a playtest is already running this only writes the file: the game watches it and
        /// reloads itself, so a second process would be wrong rather than merely wasteful.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The executable could not be resolved.</exception>
        bool Play(string? executablePath, string levelXml);
    }
}
