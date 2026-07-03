using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>Reads installed content assets by manifest-relative POSIX path, independent of platform storage.</summary>
    public interface IContentStore
    {
        /// <summary>True when the asset at <paramref name="relPath"/> is present.</summary>
        Task<bool> ExistsAsync(string relPath);

        /// <summary>Reads the asset's raw bytes. Throws if it is absent.</summary>
        Task<byte[]> ReadBytesAsync(string relPath);

        /// <summary>Reads the asset as UTF-8 text. Throws if it is absent.</summary>
        Task<string> ReadTextAsync(string relPath);

        /// <summary>True when the manifest is present and every file it lists resolves.</summary>
        Task<bool> IsPopulatedAsync();
    }
}
