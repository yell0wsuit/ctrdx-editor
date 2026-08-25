// Development build: no offline support, so a stale cache can never shadow a rebuild.
// The published build swaps in service-worker.published.js.
//
// Avalonia's save-file polyfill streams through whatever service worker is registered, so even a
// worker that caches nothing has to speak that protocol or "Save level as" breaks. See
// sw-file-save.js.
self.importScripts("./sw-file-save.js");

self.addEventListener("message", (event) => offerFileSave(event.data));

self.addEventListener("fetch", (event) => {
    const response = respondToFileSave(event.request);
    if (response !== undefined) {
        event.respondWith(response);
    }
});
