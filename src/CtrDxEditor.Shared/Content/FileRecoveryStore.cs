using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>Recovery store backed by a JSON file on the local filesystem (desktop).</summary>
    /// <remarks>
    /// Writes go to a sibling temp file that is then moved over the real one, so a crash mid-write
    /// leaves the previous snapshot intact rather than a truncated file.
    /// </remarks>
    public sealed class FileRecoveryStore(string path) : IRecoveryStore
    {
        /// <inheritdoc />
        public async Task<RecoverySnapshot?> LoadAsync()
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                string json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize(json, AppJsonContext.Default.RecoverySnapshot);
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public async Task SaveAsync(RecoverySnapshot snapshot)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                _ = Directory.CreateDirectory(dir);
            }
            string temp = path + ".tmp";
            await File.WriteAllTextAsync(
                temp, JsonSerializer.Serialize(snapshot, AppJsonContext.Default.RecoverySnapshot));
            File.Move(temp, path, overwrite: true);
        }

        /// <inheritdoc />
        public Task ClearAsync()
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A snapshot that cannot be deleted is offered again next launch; nothing is lost.
            }
            return Task.CompletedTask;
        }
    }
}
