using System;

using Avalonia;
using Avalonia.Controls;

namespace CtrDxEditor.Views
{
    /// <summary>
    /// Reads platform safe-area insets — the notch, home indicator, and browser chrome that overlay the
    /// window. Desktop reports nothing, so the zero fallback is the normal case there rather than an error.
    /// </summary>
    /// <remarks>
    /// iOS Safari changes these values with orientation and browser chrome. Device measurements on
    /// 2026-07-21 showed <c>InsetsManager</c> under-reporting the landscape right and bottom edges, so a
    /// head that can read the current CSS values supplies <see cref="PlatformSource"/>, which takes precedence.
    /// </remarks>
    public static class SafeAreaProbe
    {
        /// <summary>
        /// Platform-supplied inset reader, preferred over <c>InsetsManager</c> when set. The browser head
        /// sets this to a CSS <c>env(safe-area-inset-*)</c> reader; desktop leaves it null.
        /// </summary>
        public static Func<Thickness>? PlatformSource { get; set; }

        /// <summary>Current safe-area padding for the visual's window, or zero where unsupported.</summary>
        /// <param name="visual">Any visual attached to the window being measured.</param>
        /// <returns>The inset padding in logical pixels; zero when nothing can report insets.</returns>
        public static Thickness Read(Visual visual)
        {
            if (PlatformSource is { } source)
            {
                try
                {
                    return source();
                }
                catch (Exception)
                {
                    // The source calls into JavaScript; a host that cannot answer must not take the app
                    // down over layout padding. Zero degrades to "no insets", which is merely cosmetic.
                    return new Thickness(0);
                }
            }

            return TopLevel.GetTopLevel(visual)?.InsetsManager?.SafeAreaPadding ?? new Thickness(0);
        }
    }
}
