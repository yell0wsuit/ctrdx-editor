// Playtest transport. The game ships to the same origin as the editor, so a BroadcastChannel
// reaches it and window.open starts it. Avalonia's own Launcher cannot be used for the launch: its
// browser implementation is `openUri(u, t) { return !!window.open(u, t); }`, which throws the window
// handle away, and the handle is the only reliable way to tell whether the game is still open.

const CHANNEL = "ctrdx-playtest";

let channel = null;
let inbox = [];
let gameWindow = null;

/** Opens the channel and starts queueing messages. Safe to call more than once. */
export function open() {
    if (channel) {
        return;
    }
    channel = new BroadcastChannel(CHANNEL);
    channel.onmessage = (event) => {
        if (typeof event.data === "string") {
            inbox.push(event.data);
        }
    };
}

/** Posts one JSON message. A no-op before open(). */
export function post(json) {
    channel?.postMessage(json);
}

/** Takes every message queued since the last call. */
export function drain() {
    if (inbox.length === 0) {
        return [];
    }
    const taken = inbox;
    inbox = [];
    return taken;
}

/**
 * Opens the game with a playtest nonce and keeps the handle.
 *
 * The URL is derived from this page's own location rather than configured, so the same-origin rule
 * the channel depends on cannot silently drift: if the game were ever served from somewhere else,
 * the channel would be dead too.
 *
 * @returns {boolean} false when the browser blocked the popup.
 */
export function launch(nonce) {
    const url = new URL("../cuttherope-dx/", globalThis.location.href);
    url.searchParams.set("playtest", nonce);
    gameWindow = globalThis.open(url.href, "ctrdx-playtest");
    return gameWindow !== null;
}

/** Whether a game window opened by launch() is still open. */
export function isGameOpen() {
    return gameWindow !== null && !gameWindow.closed;
}
