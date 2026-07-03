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

        /// <summary>Returns whether a key exists in IndexedDB.</summary>
        [JSImport("hasKey", "idb")]
        public static partial Task<bool> HasKey(string key);
    }
}
