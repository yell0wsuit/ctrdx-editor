using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        // How long a freshly launched game has to emit its stdout handshake before it is judged not to
        // be a compatible Cut the Rope: DX. The game prints the line before its run loop starts, so it
        // normally arrives within about a second; the window is generous because a miss only warns - it
        // never kills the process - so a slow cold start is never interrupted by mistake.
        private static readonly TimeSpan HandshakeGracePeriod = TimeSpan.FromSeconds(10);

        private readonly PlaytestTempStore _temp = new(tempRootOverride);
        private readonly Lock _gate = new();
        private Process? _current;
        private bool _disposed;

        /// <inheritdoc />
        public event EventHandler<PlaytestExitedEventArgs>? Exited;

        /// <inheritdoc />
        public event EventHandler<PlaytestUnsupportedEventArgs>? Unsupported;

        /// <inheritdoc />
        /// <remarks>
        /// Never raised here. A desktop game reports a level it cannot load by writing to standard
        /// error and exiting non-zero, which already surfaces through <see cref="Exited"/>.
        /// </remarks>
#pragma warning disable CS0067 // Raised only by heads whose transport carries diagnostics back.
        public event EventHandler<PlaytestLevelRejectedEventArgs>? LevelRejected;
#pragma warning restore CS0067

        /// <inheritdoc />
        /// <remarks>True: there is no way to find an arbitrary user's game install.</remarks>
        public bool RequiresLocation => true;

        /// <inheritdoc />
        public bool Play(string? executablePath, string levelXml)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(executablePath);

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
                // Watched for the game's handshake line, which is how a compatible Cut the Rope: DX
                // proves it understood --level. Also drained so a full pipe cannot block the game.
                RedirectStandardOutput = true,
                // The game resolves its content relative to its own location, so run it from there
                // rather than inheriting the editor's working directory.
                WorkingDirectory = Path.GetDirectoryName(binary) ?? "",
            };

            Process process = new() { StartInfo = info, EnableRaisingEvents = true };
            StringBuilder stderr = new();

            // Cancelled the moment the handshake arrives or the process ends, which stands the pending
            // "unsupported" timeout down. Owned by this launch and captured by every handler below, so a
            // later launch's timeout can never be cancelled by this one.
            CancellationTokenSource handshake = new();

            // Drained asynchronously: a full stderr pipe would otherwise block the game once the OS
            // buffer fills.
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    _ = stderr.AppendLine(e.Data);
                }
            };

            // The handshake is the positive signal that this build is Cut the Rope: DX and accepted the
            // level. Seeing it cancels the timeout; anything else on stdout is ignored (and drained).
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null && PlaytestHandshakeLine.TryParse(e.Data, out int _, out string _))
                {
                    handshake.Cancel();
                }
            };

            process.Exited += (_, _) =>
            {
                // Flushes the remaining redirected output; without it stderr can be truncated.
                process.WaitForExit();
                int code = process.ExitCode;
                // An exit before the grace period elapses is not a missing handshake - stand the
                // timeout down so a game the user simply closed early is never called unsupported.
                handshake.Cancel();
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
                process.BeginOutputReadLine();
            }
            catch
            {
                handshake.Cancel();
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

            ArmHandshakeTimeout(process, binary, handshake);
        }

        // Warns, once the grace period passes with no handshake, that the launched program is not a
        // compatible Cut the Rope: DX. Never kills the process: an old build has opened its normal menu
        // and a genuine game may just be slow to start, so the safe move is to tell the user, not to
        // yank a window away. The handshake token stands this down when the game identifies itself, ends,
        // or the launcher is disposed.
        private void ArmHandshakeTimeout(Process process, string binary, CancellationTokenSource handshake)
        {
            _ = Task.Delay(HandshakeGracePeriod, handshake.Token).ContinueWith(
                task =>
                {
                    if (task.IsCanceled)
                    {
                        return; // Handshake seen, process ended, or shutting down - nothing to report.
                    }

                    bool report;
                    lock (_gate)
                    {
                        // Report only if this exact process is still the running one and we are not tearing
                        // down. A reload never re-arms this, so the check also rejects a stale timeout.
                        report = !_disposed && ReferenceEquals(_current, process);
                    }

                    if (report)
                    {
                        Unsupported?.Invoke(this, new PlaytestUnsupportedEventArgs(binary));
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
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
