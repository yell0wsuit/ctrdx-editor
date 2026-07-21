using Avalonia;
using Avalonia.Controls;

namespace CtrDxEditor.Views
{
    /// <summary>
    /// Reads platform safe-area insets — the notch, home indicator, and browser chrome that overlay the
    /// window. Desktop reports nothing, so the zero fallback is the normal case there rather than an error.
    /// </summary>
    /// <remarks>
    /// Avalonia documents browser support for <c>SafeAreaPadding</c> as mobile Chromium only, so iOS Safari
    /// may report zero even with <c>viewport-fit=cover</c> set on the host page. Callers must treat zero as
    /// "unknown or genuinely none" rather than proof there is no notch.
    /// </remarks>
    public static class SafeAreaProbe
    {
        /// <summary>Current safe-area padding for the visual's window, or zero where unsupported.</summary>
        /// <param name="visual">Any visual attached to the window being measured.</param>
        /// <returns>The inset padding in logical pixels; zero when there is no top level or no inset support.</returns>
        public static Thickness Read(Visual visual)
        {
            return TopLevel.GetTopLevel(visual)?.InsetsManager?.SafeAreaPadding ?? new Thickness(0);
        }
    }
}
