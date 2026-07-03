let dbPromise;

function db() {
    return (dbPromise ??= new Promise((resolve, reject) => {
        const req = indexedDB.open("ctrdx-editor", 1);
        req.onupgradeneeded = () => req.result.createObjectStore("kv");
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    }));
}

function store(mode) {
    return db().then((d) => d.transaction("kv", mode).objectStore("kv"));
}

export async function getString(key) {
    const s = await store("readonly");
    return new Promise((res, rej) => {
        const r = s.get(key);
        r.onsuccess = () => res(r.result ?? null);
        r.onerror = () => rej(r.error);
    });
}

export async function putString(key, value) {
    const s = await store("readwrite");
    return new Promise((res, rej) => {
        const r = s.put(value, key);
        r.onsuccess = () => res();
        r.onerror = () => rej(r.error);
    });
}

export async function hasKey(key) {
    const s = await store("readonly");
    return new Promise((res, rej) => {
        const r = s.getKey(key);
        r.onsuccess = () => res(r.result !== undefined);
        r.onerror = () => rej(r.error);
    });
}
