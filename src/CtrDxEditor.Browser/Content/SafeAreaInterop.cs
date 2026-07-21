using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace CtrDxEditor.Browser.Content
{
    /// <summary>Thin managed wrapper over the safearea.js CSS inset reader.</summary>
    internal static partial class SafeAreaInterop
    {
        /// <summary>Imports the safearea.js module. Must be awaited once before any other call.</summary>
        public static Task ImportAsync()
        {
            return JSHost.ImportAsync("safearea", "../safearea.js");
        }

        /// <summary>Reads the current insets as [left, top, right, bottom] in CSS pixels.</summary>
        [JSImport("readInsets", "safearea")]
        public static partial double[] ReadInsets();
    }
}
