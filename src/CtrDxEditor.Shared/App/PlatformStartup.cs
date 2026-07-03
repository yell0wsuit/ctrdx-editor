using System;
using System.Threading.Tasks;

using CtrDxEditor.Content;

namespace CtrDxEditor
{
    /// <summary>Platform-provided services that drive application startup (one per head).</summary>
    public sealed class PlatformStartup
    {
        /// <summary>Persists and loads editor settings.</summary>
        public required ISettingsStore Settings { get; init; }

        /// <summary>Resolves the already-installed content store, or null when the user must set content up.</summary>
        public required Func<Task<IContentStore?>> ResolveInstalled { get; init; }

        /// <summary>Installs the content bundle (download or upload) for this platform.</summary>
        public required IContentInstaller Installer { get; init; }

        /// <summary>Builds a content store over freshly-installed content (used after the setup dialog succeeds).</summary>
        public required Func<IContentStore> InstalledStore { get; init; }
    }
}
