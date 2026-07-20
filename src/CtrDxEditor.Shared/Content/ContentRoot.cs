using System;
using System.IO;

namespace CtrDxEditor.Content
{
    /// <summary>Resolves the content directory in the running app's environment (settings + data location).</summary>
    public static class ContentRoot
    {
        /// <summary>Path to the persisted settings file: EditorConfig/settings.json in the user data directory.</summary>
        public static string SettingsPath =>
            Path.Combine(UserDataDirectory.Current, "EditorConfig", "settings.json");

        /// <summary>Default download destination: content/ in the user data directory.</summary>
        public static string DefaultContentDir =>
            Path.Combine(UserDataDirectory.Current, "content");

        /// <summary>Returns the resolved content directory, or null when the user must set it up.</summary>
        public static string? TryResolve()
        {
            EditorSettings settings = new FileSettingsStore(SettingsPath).LoadAsync().GetAwaiter().GetResult();
            return ContentLocation.Resolve(
                [UserDataDirectory.Current, AppContext.BaseDirectory], settings.ContentPath);
        }
    }
}
