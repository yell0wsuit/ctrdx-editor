using System;
using System.IO;

namespace CtrDxEditor.Content
{
    /// <summary>Resolves the content directory in the running app's environment (settings + exe location).</summary>
    public static class ContentRoot
    {
        /// <summary>Path to the persisted settings file: EditorConfig/settings.json next to the executable.</summary>
        public static string SettingsPath =>
            Path.Combine(AppContext.BaseDirectory, "EditorConfig", "settings.json");

        /// <summary>Default download destination: content/ next to the executable.</summary>
        public static string DefaultContentDir =>
            Path.Combine(AppContext.BaseDirectory, "content");

        /// <summary>Returns the resolved content directory, or null when the user must set it up.</summary>
        public static string? TryResolve()
        {
            EditorSettings settings = new FileSettingsStore(SettingsPath).LoadAsync().GetAwaiter().GetResult();
            return ContentLocation.Resolve(AppContext.BaseDirectory, settings.ContentPath);
        }
    }
}
