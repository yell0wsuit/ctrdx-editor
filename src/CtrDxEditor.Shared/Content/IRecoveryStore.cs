using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>Persists the single unsaved-work <see cref="RecoverySnapshot"/>, independent of platform storage.</summary>
    public interface IRecoveryStore
    {
        /// <summary>Loads the snapshot, returning null when none is stored or it cannot be read.</summary>
        Task<RecoverySnapshot?> LoadAsync();

        /// <summary>Replaces the stored snapshot with <paramref name="snapshot"/>.</summary>
        Task SaveAsync(RecoverySnapshot snapshot);

        /// <summary>Removes the stored snapshot, if any.</summary>
        Task ClearAsync();
    }
}
