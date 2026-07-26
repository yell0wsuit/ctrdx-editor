using System;

using Avalonia;
using Avalonia.Media.Fonts;

namespace CtrDxEditor.Startup
{
    /// <summary>Registers the Inter faces embedded in the shared editor assembly.</summary>
    public static class BundledInterFont
    {
        /// <summary>Adds the app-owned Inter resources to Avalonia's font manager.</summary>
        /// <param name="appBuilder">Application builder receiving the font collection.</param>
        /// <returns>The same builder for fluent startup configuration.</returns>
        public static AppBuilder WithBundledInterFont(this AppBuilder appBuilder)
        {
            ArgumentNullException.ThrowIfNull(appBuilder);

            return appBuilder.ConfigureFonts(fontManager =>
                fontManager.AddFontCollection(new EmbeddedFontCollection(
                    new Uri("fonts:Inter", UriKind.Absolute),
                    new Uri("avares://CtrDxEditor.Shared/Assets/Fonts/Inter", UriKind.Absolute))));
        }
    }
}
