using System.Text.Json;
using System.Threading.Tasks;

using CtrDxEditor.Content;

namespace CtrDxEditor.Browser.Content
{
    /// <summary>Settings store backed by an IndexedDB JSON string.</summary>
    public sealed class IndexedDbSettingsStore : ISettingsStore
    {
        private const string Key = "settings";

        /// <inheritdoc />
        public async Task<EditorSettings> LoadAsync()
        {
            string? json = await IndexedDb.GetString(Key);
            return string.IsNullOrEmpty(json)
                ? new EditorSettings()
                : JsonSerializer.Deserialize(json, AppJsonContext.Default.EditorSettings) ?? new EditorSettings();
        }

        /// <inheritdoc />
        public Task SaveAsync(EditorSettings settings)
        {
            return IndexedDb.PutString(Key, JsonSerializer.Serialize(settings, AppJsonContext.Default.EditorSettings));
        }
    }
}
