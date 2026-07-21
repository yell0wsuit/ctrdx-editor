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
    /// Measured on iOS Safari 2026-07-21, Avalonia's <c>InsetsManager</c> reported left 48 / top 0 /
    /// right 20 / bottom 0 where the browser reported left 48 / top 0 / right 48 / bottom 20 — the right
    /// inset dropped and the bottom value shifted into it. A head that can read the insets correctly
    /// therefore supplies <see cref="PlatformSource"/>, which takes precedence.
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
