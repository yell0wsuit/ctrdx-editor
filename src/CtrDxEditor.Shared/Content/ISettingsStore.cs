using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>Loads and persists <see cref="EditorSettings"/>, independent of platform storage.</summary>
    public interface ISettingsStore
    {
        /// <summary>Loads settings, returning empty settings when none are stored.</summary>
        Task<EditorSettings> LoadAsync();

        /// <summary>Persists <paramref name="settings"/>.</summary>
        Task SaveAsync(EditorSettings settings);
    }
}
