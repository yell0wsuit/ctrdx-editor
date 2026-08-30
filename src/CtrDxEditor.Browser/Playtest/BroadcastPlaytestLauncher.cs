using System;

using Avalonia.Threading;

using CtrDxEditor.Playtest;

namespace CtrDxEditor.Browser.Playtest
{
    /// <summary>Plays levels in the browser build of Cut the Rope: DX over a BroadcastChannel.</summary>
    /// <remarks>
    /// The browser counterpart of the desktop process launcher, and it keeps that type's contract
    /// exactly: <see cref="Play"/> returns true for a cold launch and false for a reload, so the
    /// shared view layer needs no per-platform branch.
    /// <para>
    /// The level travels in the message rather than through any storage. That matches desktop, where
    /// the temp level file is deleted when the editor session ends - level availability is tied to
    /// editor liveness on both heads - and it avoids leaving a stale copy of somebody's level sitting
    /// in browser storage forever.
    /// </para>
    /// </remarks>
    public sealed class BroadcastPlaytestLauncher : IPlaytestLauncher, IBlockableLauncher
    {
        /// <summary>
        /// How long a freshly opened game has to announce itself before it is judged not to be a
        /// compatible build. Generous, but it does not have to cover the game's ~56 MB content
        /// download: the game announces itself before it starts loading content.
        /// </summary>
        private static readonly TimeSpan HandshakeGracePeriod = TimeSpan.FromSeconds(15);

        /// <summary>How often to check the channel for replies.</summary>
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

        // A dispatcher timer rather than a System.Timers one: this head is single-threaded, and the
        // events raised from Drain land straight on the UI thread the view already marshals to.
        private readonly DispatcherTimer _poll = new() { Interval = PollInterval };
        private string _nonce = "";

        /// <summary>
        /// The level the current session is playing. Kept after delivery rather than cleared, so a
        /// game window the user reloads can be answered when it announces itself a second time.
        /// </summary>
        private string? _sessionXml;
        private DateTime _handshakeDeadline = DateTime.MaxValue;
        private bool _handshook;
        private bool _disposed;

        /// <summary>Creates a launcher and opens the channel.</summary>
        public BroadcastPlaytestLauncher()
        {
            PlaytestInterop.Open();
            _poll.Tick += (_, _) => Drain();
            _poll.Start();
        }

        /// <inheritdoc />
        public event EventHandler<PlaytestExitedEventArgs>? Exited;

        /// <inheritdoc />
        public event EventHandler<PlaytestUnsupportedEventArgs>? Unsupported;

        /// <inheritdoc />
        public event EventHandler<PlaytestLevelRejectedEventArgs>? LevelRejected;

        /// <inheritdoc />
        /// <remarks>False: the game is a URL on this origin, not a program the user picks.</remarks>
        public bool RequiresLocation => false;

        /// <summary>Whether the launch attempt was blocked by the browser's popup blocker.</summary>
        /// <remarks>
        /// Read by the view straight after <see cref="Play"/> so it can offer the user a fresh click.
        /// See the remarks on <see cref="Play"/> for when this can happen.
        /// </remarks>
        public bool LastLaunchBlocked { get; private set; }

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// This method is synchronous, and a cold launch cannot wait for the game to boot. So the XML
        /// is stashed and posted when the game announces itself; a second Play arriving first replaces
        /// the stash rather than queueing, because only the newest level is worth sending. The same
        /// stash-and-replace applies to a reload that lands before the game has handshaken: the window
        /// exists but nothing on the other end is subscribed to the channel yet, so a post made in that
        /// window would be silently dropped rather than queued.
        /// </para>
        /// <para>
        /// <b>The window must be opened without an intervening await.</b> Browsers only allow
        /// <c>window.open</c> while a user gesture is still on the stack. Awaiting an already-completed
        /// task continues synchronously and keeps the gesture, which is why the warning-free path works;
        /// awaiting a real dialog does not, and <see cref="LastLaunchBlocked"/> is how the caller
        /// recovers. Adding an await ahead of this call breaks the common path silently.
        /// </para>
        /// </remarks>
        public bool Play(string? executablePath, string levelXml)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            LastLaunchBlocked = false;

            if (PlaytestInterop.IsGameOpen())
            {
                // Remembered on every Play, not just the cold launch: after a few hot reloads this
                // is what a refreshed window must be given, and the level from the original launch
                // would be the wrong one.
                _sessionXml = levelXml;

                // Post directly only once the game has announced itself. Between window.open and the
                // game's own subscribe there is a gap of seconds while its runtime boots, and a
                // BroadcastChannel drops messages nobody is listening for yet - during that window
                // OnReady delivers it instead.
                if (_handshook)
                {
                    PlaytestInterop.Post(PlaytestChannelMessage.FormatLevel(_nonce, levelXml));
                }

                return false;
            }

            _nonce = Guid.NewGuid().ToString("N")[..8];
            _sessionXml = levelXml;
            _handshook = false;
            _handshakeDeadline = DateTime.UtcNow + HandshakeGracePeriod;

            if (!PlaytestInterop.Launch(_nonce))
            {
                LastLaunchBlocked = true;
                _handshakeDeadline = DateTime.MaxValue;
                _sessionXml = null;
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _poll.Stop();
        }

        /// <summary>Reads whatever the game has said and acts on it.</summary>
        private void Drain()
        {
            foreach (string json in PlaytestInterop.Drain())
            {
                if (!PlaytestChannelMessage.TryParse(json, out PlaytestMessageKind kind, out string nonce, out string payload))
                {
                    continue;
                }

                switch (kind)
                {
                    case PlaytestMessageKind.Ready when string.Equals(nonce, _nonce, StringComparison.Ordinal):
                        OnReady(payload);
                        break;
                    case PlaytestMessageKind.Ready:
                        break;
                    case PlaytestMessageKind.Error when IsOurs(nonce):
                        LevelRejected?.Invoke(this, new PlaytestLevelRejectedEventArgs(payload));
                        break;
                    case PlaytestMessageKind.Error:
                        break;
                    case PlaytestMessageKind.Bye when IsOurs(nonce):
                        Exited?.Invoke(this, new PlaytestExitedEventArgs(0, string.Empty));
                        break;
                    case PlaytestMessageKind.Bye:
                        break;
                    case PlaytestMessageKind.Unknown:
                    case PlaytestMessageKind.Level:
                    default:
                        break;
                }
            }

            // A game that never announces itself is a build too old to understand ?playtest=, which
            // ignores the parameter and opens the normal game. That is exactly what the desktop head
            // reports when no stdout handshake arrives.
            if (!_handshook && DateTime.UtcNow > _handshakeDeadline)
            {
                _handshakeDeadline = DateTime.MaxValue;
                _sessionXml = null;
                Unsupported?.Invoke(this, new PlaytestUnsupportedEventArgs("cuttherope-dx (browser)"));
            }
        }

        /// <summary>Whether a message belongs to this launcher's playtest session.</summary>
        /// <param name="nonce">The nonce the message carried, empty when it carried none.</param>
        /// <returns><see langword="true"/> when this launcher should act on the message.</returns>
        /// <remarks>
        /// Deliberately lenient about an absent nonce. A game build that predates per-session
        /// addressing sends none, and dropping its messages would silently stop reporting rejected
        /// levels; treating them as ours preserves exactly today's behaviour. Once both sides carry a
        /// nonce, two concurrent playtests stop crossing wires.
        /// </remarks>
        private bool IsOurs(string nonce)
        {
            return nonce.Length == 0 || string.Equals(nonce, _nonce, StringComparison.Ordinal);
        }

        /// <summary>Answers a session that has announced itself, handing over the level it is waiting for.</summary>
        /// <param name="handshakeLine">The <c>ctrdx-playtest</c> line the game announced.</param>
        private void OnReady(string handshakeLine)
        {
            // Parsed with the same parser that reads the desktop game's stdout: a build that greets us
            // with something malformed is no more trustworthy than one that says nothing.
            if (!PlaytestHandshakeLine.TryParse(handshakeLine, out _, out _))
            {
                return;
            }

            _handshook = true;
            _handshakeDeadline = DateTime.MaxValue;

            // Deliberately not cleared after sending. A playtest window the user refreshes boots
            // again and announces itself a second time; answering that with the level this session is
            // playing is what stops a refresh dropping the user into the normal game.
            if (_sessionXml is { } xml)
            {
                PlaytestInterop.Post(PlaytestChannelMessage.FormatLevel(_nonce, xml));
            }
        }
    }
}
