// SHA-256 over a byte array using the browser's native SubtleCrypto, which runs
// off the main thread. Returns lowercase hex to match the desktop hashing path.
export async function sha256Hex(bytes) {
    const digest = await crypto.subtle.digest("SHA-256", bytes);
    return [...new Uint8Array(digest)]
        .map((b) => b.toString(16).padStart(2, "0"))
        .join("");
}
