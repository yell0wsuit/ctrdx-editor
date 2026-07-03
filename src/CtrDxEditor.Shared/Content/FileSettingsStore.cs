using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>Settings store backed by a JSON file on the local filesystem (desktop).</summary>
    public sealed class FileSettingsStore(string path) : ISettingsStore
    {
        /// <inheritdoc />
        public async Task<EditorSettings> LoadAsync()
        {
            try
            {
                if (!File.Exists(path))
                {
                    return new EditorSettings();
                }
                string json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize(json, AppJsonContext.Default.EditorSettings)
                    ?? new EditorSettings();
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                return new EditorSettings();
            }
        }

        /// <inheritdoc />
        public async Task SaveAsync(EditorSettings settings)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                _ = Directory.CreateDirectory(dir);
            }
            await File.WriteAllTextAsync(
                path, JsonSerializer.Serialize(settings, AppJsonContext.Default.EditorSettings));
        }
    }
}
