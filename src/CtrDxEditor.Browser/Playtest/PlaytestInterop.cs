using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace CtrDxEditor.Browser.Playtest
{
    /// <summary>Thin managed wrapper over the playtest.js BroadcastChannel module.</summary>
    internal static partial class PlaytestInterop
    {
        /// <summary>Imports playtest.js. Must be awaited once before any other call.</summary>
        /// <returns>A task that completes when the module is available.</returns>
        public static Task ImportAsync()
        {
            return JSHost.ImportAsync("playtest", "../playtest.js");
        }

        /// <summary>Opens the channel and begins queueing incoming messages.</summary>
        [JSImport("open", "playtest")]
        public static partial void Open();

        /// <summary>Posts one JSON message on the channel.</summary>
        /// <param name="json">Message text from <see cref="CtrDxEditor.Playtest.PlaytestChannelMessage"/>.</param>
        [JSImport("post", "playtest")]
        public static partial void Post(string json);

        /// <summary>Takes every message queued since the previous call.</summary>
        /// <returns>The queued messages, oldest first; empty when none arrived.</returns>
        [JSImport("drain", "playtest")]
        public static partial string[] Drain();

        /// <summary>Opens the game in a new window with a playtest nonce.</summary>
        /// <param name="nonce">The session nonce to launch with.</param>
        /// <returns>False when the browser blocked the popup.</returns>
        [JSImport("launch", "playtest")]
        public static partial bool Launch(string nonce);

        /// <summary>Whether a launched game window is still open.</summary>
        /// <returns>True while the window exists and has not been closed.</returns>
        [JSImport("isGameOpen", "playtest")]
        public static partial bool IsGameOpen();
    }
}
