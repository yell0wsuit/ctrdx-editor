using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

using CtrDxEditor.Playtest;

namespace CtrDxEditor.Desktop.Playtest
{
    /// <summary>Runs Cut the Rope: DX as a child process, one playtest at a time.</summary>
    /// <remarks>
    /// The only place in the editor that starts a process, keeping <see cref="Process"/> out of the
    /// WASM head. Owns the temp store, so the view layer never handles playtest file paths.
    /// </remarks>
    /// <param name="tempRootOverride">Container for session directories; defaults to the system temp root.</param>
    public sealed class ProcessPlaytestLauncher(string? tempRootOverride = null) : IPlaytestLauncher
    {
        private readonly PlaytestTempStore _temp = new(tempRootOverride);
        private readonly Lock _gate = new();
        private Process? _current;
        private bool _disposed;

        /// <inheritdoc />
        public event EventHandler<PlaytestExitedEventArgs>? Exited;

        /// <inheritdoc />
        public bool Play(string executablePath, string levelXml)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Resolved before the write so an unusable executable fails without leaving a level file
            // behind that a later, valid launch would silently inherit.
            if (!DxExecutableResolver.TryResolve(executablePath, out string binary, out string? error))
            {
                throw new InvalidOperationException(error);
            }

            // The write is the update channel: a running game watches this exact path and reloads.
            string levelPath = _temp.Write(levelXml);

            lock (_gate)
            {
                if (_current is not null)
                {
                    return false; // Already playing; the write above is all the running game needs.
                }
            }

            StartProcess(binary, levelPath);
            return true;
        }

        private void StartProcess(string binary, string levelPath)
        {
            ProcessStartInfo info = new(binary)
            {
                ArgumentList = { "--level", levelPath },
                UseShellExecute = false,
                RedirectStandardError = true,
                // The game resolves its content relative to its own location, so run it from there
                // rather than inheriting the editor's working directory.
                WorkingDirectory = Path.GetDirectoryName(binary) ?? "",
            };

            Process process = new() { StartInfo = info, EnableRaisingEvents = true };
            StringBuilder stderr = new();

            // Drained asynchronously: a full stderr pipe would otherwise block the game once the OS
            // buffer fills.
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    _ = stderr.AppendLine(e.Data);
                }
            };

            process.Exited += (_, _) =>
            {
                // Flushes the remaining redirected output; without it stderr can be truncated.
                process.WaitForExit();
                int code = process.ExitCode;
                lock (_gate)
                {
                    // Only clear if this is still the current process.
                    if (ReferenceEquals(_current, process))
                    {
                        _current = null;
                    }
                }
                // Suppressed during shutdown: Dispose kills the game, which surfaces as a non-zero
                // exit code, and reporting that would pop an error toast on the way out the door.
                if (!_disposed)
                {
                    Exited?.Invoke(this, new PlaytestExitedEventArgs(code, stderr.ToString().Trim()));
                }
                process.Dispose();
            };

            // Published before Start, not after. A game that exits immediately would otherwise run its
            // Exited handler first and be overwritten by the assignment, leaving _current pointing at
            // a disposed Process.
            lock (_gate)
            {
                _current = process;
            }

            try
            {
                _ = process.Start();
                process.BeginErrorReadLine();
            }
            catch
            {
                lock (_gate)
                {
                    if (ReferenceEquals(_current, process))
                    {
                        _current = null;
                    }
                }
                process.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Ends the playtest with the editor. A custom-level run never writes progress
        /// (<c>LevelProgressPersistence.ShouldPersist</c> is gated on it) and the game flushes
        /// preferences every frame, so a hard kill loses nothing. There is no portable graceful
        /// alternative in any case: <c>CloseMainWindow</c> is effectively Windows-only, and SIGTERM
        /// does not run MonoGame's exit handler.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            Process? process;
            lock (_gate)
            {
                process = _current;
                _current = null;
            }

            if (process is not null)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        // Wait before the directory below is removed, so the game has released the
                        // level file. Bounded: shutdown must not hang on a wedged process.
                        _ = process.WaitForExit(2000);
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException
                    or System.ComponentModel.Win32Exception)
                {
                    // Already gone, or not killable. Nothing left to do but clean up the files.
                }
            }

            _temp.Dispose();
        }
    }
}
