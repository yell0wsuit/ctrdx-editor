using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace CtrDxEditor.Browser.Content
{
    /// <summary>Thin managed wrapper over the idb.js IndexedDB key/value module.</summary>
    internal static partial class IndexedDb
    {
        /// <summary>Imports the idb.js module. Must be awaited once before any other call.</summary>
        public static Task ImportAsync()
        {
            return JSHost.ImportAsync("idb", "../idb.js");
        }

        /// <summary>Reads a string value from IndexedDB, or null when the key is absent.</summary>
        [JSImport("getString", "idb")]
        public static partial Task<string?> GetString(string key);

        /// <summary>Stores a string value in IndexedDB.</summary>
        [JSImport("putString", "idb")]
        public static partial Task PutString(string key, string value);

        /// <summary>
        /// Stores a byte array in IndexedDB as a Uint8Array (no base64). A MemoryView cannot be marshalled
        /// on a method that returns Task, so the bytes are first copied into a JS-side stash synchronously,
        /// then stored asynchronously.
        /// </summary>
        public static Task PutBytes(string key, byte[] value)
        {
            StashBytes(value);
            return PutStashedBytes(key);
        }

        /// <summary>Copies <paramref name="value"/> (a view over the WASM heap) into a JS-side stash, synchronously.</summary>
        [JSImport("stashBytes", "idb")]
        private static partial void StashBytes([JSMarshalAs<JSType.MemoryView>] Span<byte> value);

        /// <summary>Stores the stashed bytes under key, releasing the stash.</summary>
        [JSImport("putStashedBytes", "idb")]
        private static partial Task PutStashedBytes(string key);

        /// <summary>
        /// Reads a byte array from IndexedDB, or an empty array when the key is absent.
        /// A byte[] cannot cross the async JS-interop boundary directly, so this fetches the value into a
        /// JS-side stash (returning its length) and then copies it into a managed buffer synchronously.
        /// </summary>
        public static async Task<byte[]> GetBytes(string key)
        {
            int length = await BeginGetBytes(key);
            if (length <= 0)
            {
                return [];
            }
            byte[] buffer = new byte[length];
            EndGetBytes(buffer);
            return buffer;
        }

        /// <summary>Fetches key's value, stashing it JS-side, and returns its byte length (0 when absent or not binary).</summary>
        [JSImport("beginGetBytes", "idb")]
        private static partial Task<int> BeginGetBytes(string key);

        /// <summary>Copies the stashed bytes into <paramref name="dest"/> (a view over the WASM heap) and releases the stash.</summary>
        [JSImport("endGetBytes", "idb")]
        private static partial void EndGetBytes([JSMarshalAs<JSType.MemoryView>] Span<byte> dest);

        /// <summary>Returns whether a key exists in IndexedDB.</summary>
        [JSImport("hasKey", "idb")]
        public static partial Task<bool> HasKey(string key);
    }
}
