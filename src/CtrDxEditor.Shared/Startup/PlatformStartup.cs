using System;
using System.Threading.Tasks;

using Avalonia;

using CtrDxEditor.Content;
using CtrDxEditor.Playtest;

namespace CtrDxEditor.Startup
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

        /// <summary>File extension (including the leading dot) used for sprite atlas images on this platform.</summary>
        public required string SpriteImageExtension { get; init; }

        /// <summary>Approximate size of this platform's asset bundle (e.g. "336 MB"), shown next to the Download button.</summary>
        public required string DownloadSizeLabel { get; init; }

        /// <summary>Direct download URL for this platform's asset bundle, opened in the browser by Download Manually.</summary>
        public required string ManualDownloadUrl { get; init; }

        /// <summary>Whether the in-app direct download is offered; false where a cross-origin fetch is blocked (the browser), leaving only manual download + zip upload.</summary>
        public bool AllowDirectDownload { get; init; } = true;

        /// <summary>Plays levels in Cut the Rope: DX for playtesting; null where processes cannot be spawned (the browser).</summary>
        public IPlaytestLauncher? Playtest { get; init; }

        /// <summary>
        /// Reports whether a newer release has been published, offering the user the release page at
        /// startup. Null where the head is not user-updatable (the browser, which always loads the
        /// deployed build), which skips the check entirely.
        /// </summary>
        public Func<Task<bool>>? CheckForUpdate { get; init; }

        /// <summary>
        /// Reads platform safe-area insets, when this head can do so more reliably than Avalonia's
        /// <c>InsetsManager</c>. Null on heads that have nothing better (desktop), where the manager is used.
        /// </summary>
        public Func<Thickness>? SafeAreaInsets { get; init; }
    }
}
