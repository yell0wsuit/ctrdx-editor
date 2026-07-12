using System;
using System.Threading.Tasks;

using CtrDxEditor.Content;

namespace CtrDxEditor.Tests
{
    /// <summary>A content store that reports nothing populated — for VM tests without assets.</summary>
    public sealed class EmptyContentStore : IContentStore
    {
        /// <summary>Reports that a relative content path does not exist.</summary>
        /// <param name="relPath">The relative content path to inspect.</param>
        /// <returns>A completed task whose result is <see langword="false"/>.</returns>
        public Task<bool> ExistsAsync(string relPath)
        {
            return Task.FromResult(false);
        }

        /// <summary>Returns empty binary content for a relative path.</summary>
        /// <param name="relPath">The relative content path to read.</param>
        /// <returns>A completed task containing an empty byte array.</returns>
        public Task<byte[]> ReadBytesAsync(string relPath)
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        /// <summary>Returns empty text content for a relative path.</summary>
        /// <param name="relPath">The relative content path to read.</param>
        /// <returns>A completed task containing an empty string.</returns>
        public Task<string> ReadTextAsync(string relPath)
        {
            return Task.FromResult("");
        }

        /// <summary>Reports that the store has no installed content.</summary>
        /// <returns>A completed task whose result is <see langword="false"/>.</returns>
        public Task<bool> IsPopulatedAsync()
        {
            return Task.FromResult(false);
        }
    }
}
