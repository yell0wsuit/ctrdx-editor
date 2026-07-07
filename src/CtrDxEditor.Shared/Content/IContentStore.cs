using System;
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

        /// <summary>
        /// Reads the asset's raw bytes synchronously, from data the store already holds in memory (or on
        /// local disk). Throws if it is absent. Unlike <see cref="ReadBytesAsync"/> this never awaits, so
        /// it is safe to call from the UI thread on single-threaded WebAssembly, where blocking on an
        /// async read deadlocks the sole thread. Stores that cannot serve reads synchronously throw.
        /// </summary>
        byte[] ReadBytes(string relPath)
        {
            throw new NotSupportedException("This content store does not support synchronous reads.");
        }

        /// <summary>Reads the asset as UTF-8 text synchronously. See <see cref="ReadBytes"/>.</summary>
        string ReadText(string relPath)
        {
            throw new NotSupportedException("This content store does not support synchronous reads.");
        }
    }
}
