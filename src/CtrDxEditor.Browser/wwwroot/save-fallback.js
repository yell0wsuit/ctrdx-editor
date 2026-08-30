// Keeps Avalonia's save-file polyfill on its Blob fallback.
//
// Firefox has no File System Access API, so SaveFilePickerAsync falls back to the polyfill
// Avalonia bundles (native-file-system-adapter). That polyfill has two ways to deliver the bytes,
// and it chooses between them on whether navigator.serviceWorker.getRegistration() finds anything:
//
//   - nothing registered (or Safari, which it sniffs for): collect the chunks into a Blob and
//     click an object URL.
//   - a registration: stream the file through that worker, triggering the download by pointing a
//     hidden iframe at a URL the worker is expected to answer.
//
// Registering a worker for offline support therefore moved every Firefox save onto the streamed
// path, which cannot work in Avalonia 12.1.1. Two independent faults, both in code the app cannot
// reach:
//
//   - The polyfill's sink returns its ready promise from its own start(), so the first write is
//     held until the worker pulls. Avalonia's worker only pulls once a chunk has arrived - a chunk
//     that never comes - so the two deadlock and the file lands at zero bytes.
//   - StreamHelper.write hands the polyfill a Uint8Array pointing straight into the WebAssembly
//     heap, and the sink posts it with [chunk.buffer] in the transfer list. Transferring the wasm
//     ArrayBuffer throws ("cannot transfer WebAssembly ArrayBuffer"), so a worker that pulled
//     correctly would still be fed nothing.
//
// The Blob fallback has neither problem - new Blob([chunk]) copies the bytes there and then - and
// it is the path Safari has always taken here. Hiding the registration from the polyfill is what
// puts Firefox back on it. Levels are kilobytes and a screenshot is a few megabytes, so buffering
// one save in memory costs nothing worth streaming for.
//
// Only the polyfill reads getRegistration - nothing in Avalonia or in this app otherwise calls it,
// and the offline cache and the update prompt work off the registration that pwa.js already holds.
// The override installs only where the native picker is missing, so Chromium, which never enters
// the polyfill at all, is left untouched.

if (!("showSaveFilePicker" in globalThis) && navigator.serviceWorker) {
    try {
        Object.defineProperty(navigator.serviceWorker, "getRegistration", {
            configurable: true,
            writable: true,
            value: () => Promise.resolve(undefined),
        });
    } catch (error) {
        // Losing this costs saving, so it is worth a console note, but throwing here would cost
        // the whole page its scripts.
        console.warn("could not force the save-file blob fallback:", error);
    }
}
