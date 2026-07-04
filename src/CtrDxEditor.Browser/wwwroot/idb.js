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

export const putBytes = async (key, value) => {
    const s = await store("readwrite");
    return new Promise((res, rej) => {
        const r = s.put(value, key);
        r.onsuccess = () => res();
        r.onerror = () => rej(r.error);
    });
};

export const getBytes = async (key) => {
    const s = await store("readonly");
    return new Promise((res, rej) => {
        const r = s.get(key);
        // Ignore a legacy base64 string (or any non-binary value) under this key so an old dev
        // install falls back to setup instead of a marshalling crash.
        r.onsuccess = () =>
            res(r.result instanceof Uint8Array ? r.result : null);
        r.onerror = () => rej(r.error);
    });
};

export const hasKey = async (key) => {
    const s = await store("readonly");
    return new Promise((res, rej) => {
        const r = s.getKey(key);
        r.onsuccess = () => res(r.result !== undefined);
        r.onerror = () => rej(r.error);
    });
};
