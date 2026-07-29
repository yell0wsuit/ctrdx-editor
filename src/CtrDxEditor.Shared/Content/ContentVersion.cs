using System;
using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>
    /// Decides whether an installed asset bundle is old enough to be worth re-downloading.
    /// </summary>
    /// <remarks>
    /// Complements the tolerant sprite preload rather than replacing it. Preload already keeps a stale
    /// bundle from breaking the editor - objects it has no art for simply leave the palette - so this
    /// only has to tell the user that re-downloading would get those objects back. It also covers the
    /// one case presence checks cannot see: an atlas whose filename is unchanged but whose contents
    /// gained frames.
    /// </remarks>
    public static class ContentVersion
    {
        /// <summary>
        /// The bundle revision this editor build wants.
        /// </summary>
        /// <remarks>
        /// Bump this in the same change that publishes a bundle declaring the new revision - never
        /// ahead of it. Raising it first tells every installed copy to re-download content that does
        /// not exist yet, and the prompt has no permanent dismissal.
        /// <para>
        /// Revision 1 is the baseline: the first bundles to carry a <c>version</c> field at all. Anyone
        /// still holding a bundle from before it reads as 0 and is asked to re-download once.
        /// </para>
        /// </remarks>
        public const int CurrentAssetVersion = 1;

        /// <summary>Whether an installed bundle predates the revision this build wants.</summary>
        /// <param name="installed">Revision declared by the installed bundle's manifest.</param>
        /// <returns><see langword="true"/> when re-downloading would gain the user something.</returns>
        public static bool IsOutdated(int installed)
        {
            return installed < CurrentAssetVersion;
        }

        /// <summary>
        /// Reads the installed bundle's revision and reports whether it is behind this build.
        /// </summary>
        /// <param name="store">Store over the installed content.</param>
        /// <returns><see langword="true"/> when the bundle is outdated.</returns>
        /// <remarks>
        /// Never throws, and never guesses. A manifest that is absent, unreadable, or unintelligible
        /// leaves the bundle alone: the editor cannot tell whether it is behind, and an unprompted
        /// several-hundred-megabyte download is the wrong way to resolve that doubt. Only a manifest
        /// that parses and reports a revision older than <see cref="CurrentAssetVersion"/> counts.
        /// </remarks>
        public static async Task<bool> IsOutdatedAsync(IContentStore store)
        {
            try
            {
                string json = await store.ReadTextAsync(ContentManifest.FileName);
                return ContentManifest.ParseVersion(json) is { } installed && IsOutdated(installed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CtrDx] Could not read the installed content revision.\n{ex}");
                return false;
            }
        }
    }
}
