using System.Text.Json;
using System.Threading.Tasks;

using CtrDxEditor.Content;

namespace CtrDxEditor.Browser.Content
{
    /// <summary>Recovery store backed by an IndexedDB JSON string.</summary>
    /// <remarks>
    /// Clearing writes an empty string rather than deleting the key, because the idb.js module exposes
    /// no delete; an empty value reads back as no snapshot.
    /// </remarks>
    public sealed class IndexedDbRecoveryStore : IRecoveryStore
    {
        private const string Key = "recovery";

        /// <inheritdoc />
        public async Task<RecoverySnapshot?> LoadAsync()
        {
            string? json = await IndexedDb.GetString(Key);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize(json, AppJsonContext.Default.RecoverySnapshot);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public Task SaveAsync(RecoverySnapshot snapshot)
        {
            return IndexedDb.PutString(Key, JsonSerializer.Serialize(snapshot, AppJsonContext.Default.RecoverySnapshot));
        }

        /// <inheritdoc />
        public Task ClearAsync()
        {
            return IndexedDb.PutString(Key, string.Empty);
        }
    }
}
