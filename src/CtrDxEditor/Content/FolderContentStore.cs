using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>Content store backed by a directory on the local filesystem (desktop).</summary>
    public sealed class FolderContentStore(string root) : IContentStore
    {
        private string Full(string relPath)
        {
            return Path.Combine(root, relPath.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string relPath)
        {
            return Task.FromResult(File.Exists(Full(relPath)));
        }

        /// <inheritdoc />
        public async Task<byte[]> ReadBytesAsync(string relPath)
        {
            return await File.ReadAllBytesAsync(Full(relPath));
        }

        /// <inheritdoc />
        public async Task<string> ReadTextAsync(string relPath)
        {
            return await File.ReadAllTextAsync(Full(relPath));
        }

        /// <inheritdoc />
        public async Task<bool> IsPopulatedAsync()
        {
            string manifestPath = Full(ContentManifest.FileName);
            if (!File.Exists(manifestPath))
            {
                return false;
            }
            IReadOnlyDictionary<string, string> manifest =
                ContentManifest.ParseFiles(await File.ReadAllTextAsync(manifestPath));
            foreach (string rel in manifest.Keys)
            {
                if (!File.Exists(Full(rel)))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
