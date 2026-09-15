// Playtest transport. The game ships to the same origin as the editor, so a BroadcastChannel
// reaches it and window.open starts it. Avalonia's own Launcher cannot be used for the launch: its
// browser implementation is `openUri(u, t) { return !!window.open(u, t); }`, which throws the window
// handle away, and the handle is the only reliable way to tell whether the game is still open.

//
// The level is also left in localStorage under the session nonce before the game opens. Opening it
// backgrounds this tab, and a mobile browser may suspend or discard a background tab before it can
// answer the game's announcement; a stored level reaches the game with this tab doing nothing.

const CHANNEL = "ctrdx-playtest";
const STORAGE_PREFIX = "ctrdx-playtest:";

/**
 * How old a stored level must be before any editor may remove it. Long enough that no live session
 * in another editor tab is swept, since an entry is refreshed on every Play.
 */
const STALE_AFTER_MS = 24 * 60 * 60 * 1000;

let channel = null;
let inbox = [];
let gameWindow = null;

/** Opens the channel and starts queueing messages. Safe to call more than once. */
export function open() {
    if (channel) {
        return;
    }
    sweepStoredLevels();
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
export function launch(nonce, levelMessage) {
    storeLevel(nonce, levelMessage);
    const url = new URL("../cuttherope-dx/", globalThis.location.href);
    url.searchParams.set("playtest", nonce);
    gameWindow = globalThis.open(url.href, "ctrdx-playtest");
    return gameWindow !== null;
}

/** Whether a game window opened by launch() is still open. */
export function isGameOpen() {
    return gameWindow !== null && !gameWindow.closed;
}

/**
 * Stores the level message a session's game reads when it boots, replacing any earlier one.
 *
 * Failing to store is not fatal: blocked site data or a full quota leaves the channel, which is
 * all a desktop browser needs.
 */
export function storeLevel(nonce, levelMessage) {
    try {
        globalThis.localStorage.setItem(
            STORAGE_PREFIX + nonce,
            JSON.stringify({ storedAt: Date.now(), message: levelMessage }),
        );
    } catch (error) {
        // A failed write leaves the previous value in place, and a game booting from that would
        // play an older revision as if it were current. No entry falls back to the channel instead.
        forgetLevel(nonce);
        console.warn("playtest level could not be stored:", error);
    }
}

/** Removes a session's stored level. */
export function forgetLevel(nonce) {
    try {
        globalThis.localStorage.removeItem(STORAGE_PREFIX + nonce);
    } catch {
        // Nothing could have been stored either.
    }
}

/**
 * Removes levels left behind by sessions that ended without saying so - an editor tab closed or
 * discarded while its game was running.
 */
function sweepStoredLevels() {
    try {
        const storage = globalThis.localStorage;
        const now = Date.now();
        const stale = [];
        for (let i = 0; i < storage.length; i++) {
            const key = storage.key(i);
            if (key === null || !key.startsWith(STORAGE_PREFIX)) {
                continue;
            }
            let storedAt = 0;
            try {
                storedAt = JSON.parse(storage.getItem(key)).storedAt ?? 0;
            } catch {
                // Malformed, so stale.
            }
            if (now - storedAt > STALE_AFTER_MS) {
                stale.push(key);
            }
        }
        // Collected first: removing while iterating shifts the indices under the loop.
        for (const key of stale) {
            storage.removeItem(key);
        }
    } catch {
        // Blocked site data: nothing was stored to sweep.
    }
}
