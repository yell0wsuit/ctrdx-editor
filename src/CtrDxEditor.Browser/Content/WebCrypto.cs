using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace CtrDxEditor.Browser.Content
{
    /// <summary>Thin managed wrapper over the webcrypto.js SubtleCrypto module.</summary>
    internal static partial class WebCrypto
    {
        /// <summary>Imports the webcrypto.js module. Must be awaited once before any other call.</summary>
        public static Task ImportAsync()
        {
            return JSHost.ImportAsync("webcrypto", "../webcrypto.js");
        }

        /// <summary>Computes the lowercase-hex SHA-256 of the given bytes off the main thread.</summary>
        [JSImport("sha256Hex", "webcrypto")]
        public static partial Task<string> Sha256HexAsync(byte[] bytes);
    }
}
