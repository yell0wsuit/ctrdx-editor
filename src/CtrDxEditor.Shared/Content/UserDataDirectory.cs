using System;
using System.IO;

namespace CtrDxEditor.Content
{
    /// <summary>
    /// Resolves the directory the editor may write settings and downloaded content into.
    /// </summary>
    /// <remarks>
    /// Candidates are tried in order and accepted only when a real write succeeds, so a read-only
    /// install (AppImage, <c>Program Files</c>) falls through without any platform-specific branch.
    /// The executable directory is preferred for portability, but skipped inside a macOS <c>.app</c>
    /// bundle, where writing would invalidate the bundle's signature.
    /// </remarks>
    public static class UserDataDirectory
    {
        /// <summary>Folder created under Documents and LocalApplicationData, but never next to the executable.</summary>
        private const string FolderName = "CtrDxEditorData";

        /// <summary>The resolved writable directory, determined once on first use.</summary>
        public static string Current { get; } = Resolve(
            AppContext.BaseDirectory,
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            IsWritable);

        /// <summary>
        /// Picks the first writable candidate: the executable directory (unless it sits inside a
        /// macOS <c>.app</c> bundle), then <c>Documents/CtrDxEditorData</c>, then
        /// <c>LocalApplicationData/CtrDxEditorData</c>, then the current directory.
        /// </summary>
        /// <param name="baseDirectory">The directory holding the executable.</param>
        /// <param name="documentsDirectory">The user's Documents directory.</param>
        /// <param name="localAppDataDirectory">The user's local application data directory.</param>
        /// <param name="isWritable">Predicate deciding whether a candidate can be written to.</param>
        /// <returns>The chosen directory; never null.</returns>
        public static string Resolve(
            string baseDirectory,
            string documentsDirectory,
            string localAppDataDirectory,
            Func<string, bool> isWritable)
        {
            if (!IsInsideMacAppBundle(baseDirectory) && isWritable(baseDirectory))
            {
                return baseDirectory;
            }

            string documents = Path.Combine(documentsDirectory, FolderName);
            if (isWritable(documents))
            {
                return documents;
            }

            string localAppData = Path.Combine(localAppDataDirectory, FolderName);
            return isWritable(localAppData) ? localAppData : ".";
        }

        /// <summary>
        /// Determines whether a path sits inside a macOS app bundle by looking for the standard
        /// <c>*.app/Contents/MacOS</c> structure in its ancestors.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns><see langword="true" /> when the path is inside a bundle; otherwise <see langword="false" />.</returns>
        public static bool IsInsideMacAppBundle(string path)
        {
            for (DirectoryInfo? dir = new(path); dir is not null; dir = dir.Parent)
            {
                if (dir.Name.Equals("MacOS", StringComparison.OrdinalIgnoreCase)
                    && dir.Parent?.Name.Equals("Contents", StringComparison.OrdinalIgnoreCase) == true
                    && dir.Parent.Parent?.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Creates the directory if needed and confirms it accepts a write. Creating alone is not
        /// proof: Windows folder virtualization can make <see cref="Directory.CreateDirectory(string)"/>
        /// appear to succeed in locations the app cannot actually use.
        /// </summary>
        /// <param name="path">The candidate directory.</param>
        /// <returns><see langword="true" /> when a file was written and removed successfully.</returns>
        public static bool IsWritable(string path)
        {
            try
            {
                _ = Directory.CreateDirectory(path);
                string probe = Path.Combine(path, $".write-probe-{Guid.NewGuid():N}");
                File.WriteAllText(probe, string.Empty);
                File.Delete(probe);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                return false;
            }
        }
    }
}
