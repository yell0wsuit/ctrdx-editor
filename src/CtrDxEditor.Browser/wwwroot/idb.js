let dbPromise;

const db = () =>
    (dbPromise ??= new Promise((resolve, reject) => {
        const req = indexedDB.open("ctrdx-editor", 1);
        req.onupgradeneeded = () => {
            if (!req.result.objectStoreNames.contains("kv")) {
                req.result.createObjectStore("kv");
            }
        };
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    }));

const store = (mode) => {
    return db().then((d) => d.transaction("kv", mode).objectStore("kv"));
};

export const getString = async (key) => {
    const s = await store("readonly");
    return new Promise((res, rej) => {
        const r = s.get(key);
        r.onsuccess = () => res(r.result ?? null);
        r.onerror = () => rej(r.error);
    });
};

export const putString = async (key, value) => {
    const s = await store("readwrite");
    return new Promise((res, rej) => {
        const r = s.put(value, key);
        r.onsuccess = () => res();
        r.onerror = () => rej(r.error);
    });
};

// Bytes cross the JS/WASM boundary as a MemoryView, which is only valid during a synchronous
// call — so writes and reads are each split into a synchronous copy plus an async store/fetch,
// bridged by a module-level stash.
let bytesStash = null;

export const stashBytes = (value) => {
    // value is a MemoryView over the WASM heap; copy it out while it is still valid.
    bytesStash = value.slice();
};

export const putStashedBytes = async (key) => {
    const bytes = bytesStash;
    bytesStash = null;
    const s = await store("readwrite");
    return new Promise((res, rej) => {
        const r = s.put(bytes, key);
        r.onsuccess = () => res();
        r.onerror = () => rej(r.error);
    });
};

export const beginGetBytes = async (key) => {
    const s = await store("readonly");
    bytesStash = await new Promise((res, rej) => {
        const r = s.get(key);
        r.onsuccess = () => res(r.result ?? null);
        r.onerror = () => rej(r.error);
    });
    return bytesStash ? bytesStash.length : 0;
};

export const endGetBytes = (dest) => {
    // dest is a MemoryView over the WASM heap sized to the stash; copy in and release.
    dest.set(bytesStash);
    bytesStash = null;
};

export const hasKey = async (key) => {
    const s = await store("readonly");
    return new Promise((res, rej) => {
        const r = s.getKey(key);
        r.onsuccess = () => res(r.result !== undefined);
        r.onerror = () => rej(r.error);
    });
};
