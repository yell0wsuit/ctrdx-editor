// Offline cache for the published editor.
//
// service-worker-assets.js is generated at publish by the static web assets SDK: every asset
// with its integrity hash, plus a version derived from those hashes.
//
// Everything the editor needs lives in one cache, named after the manifest version and replaced
// wholesale when it changes. There is no second, long-lived cache for bulk content the way the
// game has one: the editor's assets (fonts, localization, guide images) are AvaloniaResources
// compiled into the assemblies, so they arrive inside the fingerprinted runtime files and age
// with them. The version in the cache name is what retires the handful of assets that are not
// fingerprinted — index.html and the loose .js files.
//
// The cache is filled at install, so one visit is enough to make the editor work offline. That
// costs a second download of the runtime on a first visit, in parallel with the page's own boot
// downloads. Caching lazily instead avoids it, but only partly: the .NET loader fires its first
// requests within milliseconds of page load, before the worker has activated and claimed the
// page, so roughly half the runtime bypasses the worker and offline only starts working on a
// second visit. Fetching the stragglers afterwards was measured too — most revalidate for free,
// but the two largest assemblies are past the size Chromium will hold in its HTTP cache, so they
// transfer in full and most of the saving disappears.
//
// The cache prefix must not collide with the game's. GitHub Pages project sites share the
// yell0wsuit.github.io origin, Cache Storage is per-origin, and the game's activation deletes
// every cache whose name starts with "ctrdx-" but is not one of its own two. A prefix under
// that would be purged whenever the game deploys.
//
// The worker and the web manifest are deliberately absent from the cache. The browser
// revalidates this file on every navigation, which is the only thing that notices a new
// deployment; serving it from a cache it controls would leave it unable to replace itself.

self.importScripts("./service-worker-assets.js");

// Avalonia's save-file polyfill streams downloads through whichever service worker is registered
// on the page, so this one has to speak that protocol too. See sw-file-save.js.
self.importScripts("./sw-file-save.js");

const cachePrefix = "ctrdxeditor-";
const shellCacheName = `${cachePrefix}shell-${self.assetsManifest.version}`;

// Relative to the worker, so the app works from a domain root or a project subpath alike.
const scopeUrl = new URL("./", self.location.href);

// Debug symbols and source maps are shipped but never needed to run, and the files below are
// excluded for the reasons given above.
const shellExclude = [
    /^service-worker(-assets)?\.js$/,
    /^sw-file-save\.js$/,
    /^manifest\.webmanifest$/,
    // Install-dialog artwork. The browser fetches these when offering to install the app; the
    // app itself never asks for them, so caching half a megabyte of them offline buys nothing.
    /^screenshots\//,
    /\.pdb$/,
    /\.map$/,
];

const shellAssets = self.assetsManifest.assets.filter(
    (asset) => !shellExclude.some((pattern) => pattern.test(asset.url)),
);
const shellHashes = new Map(
    shellAssets.map((asset) => [new URL(asset.url, scopeUrl).href, asset.hash]),
);

self.addEventListener("install", (event) => event.waitUntil(onInstall()));
self.addEventListener("activate", (event) => event.waitUntil(onActivate()));
self.addEventListener("fetch", (event) => event.respondWith(onFetch(event)));

// Two kinds of message arrive here. Avalonia's save-file polyfill hands over a file to stream,
// and the page asks to skip waiting once the user accepts the update prompt. Until it does, a new
// worker waits, so a version never changes underneath an editing session in progress.
self.addEventListener("message", (event) => {
    if (offerFileSave(event.data)) {
        return;
    }
    if (event.data?.type === "skip-waiting") {
        self.skipWaiting();
    }
});

async function onInstall() {
    const requests = shellAssets.map(
        (asset) =>
            new Request(new URL(asset.url, scopeUrl), {
                integrity: asset.hash,
                cache: "no-cache",
            }),
    );
    const cache = await caches.open(shellCacheName);
    await cache.addAll(requests);
}

async function onActivate() {
    const keys = await caches.keys();
    await Promise.all(
        keys
            .filter((key) => key.startsWith(cachePrefix) && key !== shellCacheName)
            .map((key) => caches.delete(key)),
    );

    // Claim the page that registered this worker, so it is already under control on the very
    // first visit rather than only from the second one onwards.
    await self.clients.claim();
}

async function onFetch(event) {
    const request = event.request;
    if (request.method !== "GET") {
        return fetch(request);
    }

    // Before the navigation branch: a streamed save arrives as an iframe navigation, and serving
    // index.html to it would swallow the download.
    const fileSave = respondToFileSave(request);
    if (fileSave !== undefined) {
        return fileSave;
    }

    // A navigation to any in-scope URL is this single page.
    if (request.mode === "navigate") {
        const cached = await caches.match(new URL("index.html", scopeUrl));
        return cached ?? fetch(request);
    }

    const hash = shellHashes.get(request.url);
    if (hash !== undefined) {
        return serveShell(request, hash);
    }

    return fetch(request);
}

/**
 * Serves a shell asset. Install normally has these already; the fetch path covers a failed
 * install, which is all-or-nothing and would otherwise leave the cache empty for good.
 *
 * @param {Request} request
 * @param {string} hash Manifest hash the response has to match.
 */
async function serveShell(request, hash) {
    const cache = await caches.open(shellCacheName);
    const cached = await cache.match(request);
    if (cached) {
        return cached;
    }

    const { response, verified } = await fetchVerified(request, hash);
    if (verified && response.ok && response.status === 200) {
        await cache.put(request, response.clone());
    }
    return response;
}

/**
 * Fetches an asset and reports whether it is the one this publish expects.
 *
 * "no-cache" revalidates with the origin rather than trusting an HTTP-cached copy, and the
 * integrity check is what keeps bytes that are stale anyway from being stored under the current
 * hash — an entry no later activation could tell apart from a genuinely current one.
 *
 * @param {Request} request
 * @param {string} hash Manifest hash the response has to match.
 * @returns {Promise<{response: Response, verified: boolean}>}
 */
async function fetchVerified(request, hash) {
    try {
        const response = await fetch(
            new Request(request, { integrity: hash, cache: "no-cache" }),
        );
        return { response, verified: true };
    } catch {
        // Either the network is gone, in which case the retry fails the same way and the caller
        // sees the rejection it would have seen anyway, or the origin is still mid rollout.
        // Serving unverified bytes uncached keeps the editor usable now and leaves the next load
        // free to pick up the real ones.
        return { response: await fetch(request), verified: false };
    }
}
