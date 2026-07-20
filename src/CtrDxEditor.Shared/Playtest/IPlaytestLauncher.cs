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

    /// <summary>Plays a level in Cut the Rope: DX. Absent on platforms that cannot spawn processes.</summary>
    public interface IPlaytestLauncher : IDisposable
    {
        /// <summary>Raised when the playtest process exits. Not raised on the UI thread.</summary>
        event EventHandler<PlaytestExitedEventArgs>? Exited;

        /// <summary>Writes <paramref name="levelXml"/> to the session level file, starting the game if it is not already running.</summary>
        /// <param name="executablePath">The user-picked executable or macOS .app bundle path.</param>
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
        bool Play(string executablePath, string levelXml);
    }
}
