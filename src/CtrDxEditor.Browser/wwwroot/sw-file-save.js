// Avalonia's streamed-download protocol, which any service worker registered on this page has
// to implement.
//
// Avalonia's SaveFilePickerAsync falls back to a polyfill in browsers without the File System
// Access API (Firefox, most notably). That polyfill calls navigator.serviceWorker.getRegistration()
// and, if it finds one, streams the file out through it instead of buffering the whole thing into
// a Blob: it posts { url, headers, readablePort } to the active worker and then points a hidden
// iframe at that url, expecting the worker's fetch handler to answer with the piped stream.
// Avalonia ships _framework/sw.js for exactly this, but never registers it — it uses whatever
// registration happens to be there.
//
// So registering a caching worker at the app's scope silently hijacks that path: the polyfill
// stops using the Blob fallback and hands the save to a worker that does not know the protocol,
// and "Save level as" produces nothing. Both of this app's workers import this file for that
// reason — the inert development one included, since the polyfill only checks that a registration
// exists, not what it does.
//
// Ported from _framework/sw.js, with the pull fixed - see PortSource.pull. Keep it in step with
// Avalonia if that file's protocol changes.

/** Message types on the port shared with the page. Mirrors Avalonia's numbering. */
const CHUNK = 0;
const ERROR = 1;
const CLOSE = 2;

// The page's sink reads a bare message of this type from our side as "send more". The protocol
// reuses the one number in both directions; naming it separately keeps the two senses apart.
const PULL = CHUNK;

/** Pending saves, keyed by the url the iframe will navigate to. */
const pendingSaves = new Map();

/** Feeds a ReadableStream from the chunks the page sends over a MessagePort. */
class PortSource {
    /** @param {MessagePort} port */
    constructor(port) {
        this.port = port;
        this.port.onmessage = (event) => this.onMessage(event.data);
    }

    /** @param {ReadableStreamDefaultController} controller */
    start(controller) {
        this.controller = controller;
    }

    /**
     * Asks the page for the next chunk. This is the whole of the protocol's backpressure, and it
     * has to fire before the first chunk ever arrives: the polyfill's sink returns its ready
     * promise from its own start(), so the writer the editor holds stays blocked until a pull
     * releases it.
     *
     * Avalonia's sw.js instead posts the pull from its message handler, once a chunk has landed -
     * a chunk that, with nothing to release the writer, never comes. Page and worker then wait on
     * each other while the iframe download the polyfill has already started reads a stream that
     * stays empty, and the browser saves a zero-byte file. Firefox is where that shows: Chromium
     * has the File System Access API and never reaches this polyfill, and Safari is detected and
     * routed to the polyfill's blob fallback, so the streamed path is Firefox's alone.
     *
     * The ReadableStream calls this again whenever its queue drops back below the high-water
     * mark, which is what keeps the page from running ahead of the download.
     */
    pull() {
        this.port.postMessage({ type: PULL });
    }

    /** @param {Error} reason */
    cancel(reason) {
        this.port.postMessage({ type: ERROR, reason: reason.message });
        this.port.close();
    }

    onMessage(data) {
        if (!this.controller) {
            return;
        }
        if (data.type === CHUNK) {
            this.controller.enqueue(data.chunk);
        } else if (data.type === ERROR) {
            this.controller.error(data.reason);
            this.port.close();
        } else if (data.type === CLOSE) {
            this.controller.close();
            this.port.close();
        }
    }
}

/**
 * Registers a save the page is about to request. Ignores anything that is not the polyfill's
 * handshake — it also posts a bare 0 as a keep-alive.
 *
 * @param {unknown} data A message event's data.
 * @returns {boolean} Whether the message was a save handshake.
 */
function offerFileSave(data) {
    if (!data?.url || !data?.readablePort) {
        return false;
    }
    pendingSaves.set(data.url, {
        stream: new ReadableStream(
            new PortSource(data.readablePort),
            new CountQueuingStrategy({ highWaterMark: 4 }),
        ),
        headers: data.headers,
    });
    return true;
}

/**
 * Answers the iframe navigation for a registered save, if this request is one.
 *
 * Must be consulted before any navigation handling: the polyfill's iframe makes this a navigate
 * request, so a worker that serves its cached index.html to navigations first would swallow the
 * download.
 *
 * @param {Request} request
 * @returns {Response | undefined} The streamed file, or undefined if this is not a save.
 */
function respondToFileSave(request) {
    const save = pendingSaves.get(request.url);
    if (save === undefined) {
        return undefined;
    }
    // One url serves one save. Deleting also keeps a cancelled save from leaking.
    pendingSaves.delete(request.url);
    return new Response(save.stream, { headers: save.headers });
}
