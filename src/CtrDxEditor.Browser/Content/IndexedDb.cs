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

        /// <summary>Stores a byte array in IndexedDB as a Uint8Array (no base64).</summary>
        [JSImport("putBytes", "idb")]
        public static partial Task PutBytes(string key, byte[] value);

        /// <summary>Reads a byte array from IndexedDB, or an empty array when the key is absent or not binary.</summary>
        [JSImport("getBytes", "idb")]
        public static partial Task<byte[]> GetBytes(string key);

        /// <summary>Returns whether a key exists in IndexedDB.</summary>
        [JSImport("hasKey", "idb")]
        public static partial Task<bool> HasKey(string key);
    }
}
