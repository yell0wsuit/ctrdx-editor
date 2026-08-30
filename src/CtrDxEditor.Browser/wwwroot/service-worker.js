// Development build: no offline support, so a stale cache can never shadow a rebuild. The
// published build swaps in service-worker.published.js.
//
// Deliberately empty. pwa.js registers a worker in every build so the registration and update
// paths are exercised in development too; this one just has nothing to cache.
