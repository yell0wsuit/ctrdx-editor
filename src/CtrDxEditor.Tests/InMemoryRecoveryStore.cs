using System.Threading.Tasks;

using CtrDxEditor.Content;

namespace CtrDxEditor.Tests
{
    /// <summary>An in-memory recovery store that counts writes, for view-model tests.</summary>
    public sealed class InMemoryRecoveryStore : IRecoveryStore
    {
        /// <summary>The stored snapshot, or null when empty.</summary>
        public RecoverySnapshot? Stored { get; set; }

        /// <summary>How many times <see cref="SaveAsync"/> ran.</summary>
        public int SaveCount { get; private set; }

        /// <summary>How many times <see cref="ClearAsync"/> ran.</summary>
        public int ClearCount { get; private set; }

        /// <inheritdoc />
        public Task<RecoverySnapshot?> LoadAsync()
        {
            return Task.FromResult(Stored);
        }

        /// <inheritdoc />
        public Task SaveAsync(RecoverySnapshot snapshot)
        {
            Stored = snapshot;
            SaveCount++;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ClearAsync()
        {
            Stored = null;
            ClearCount++;
            return Task.CompletedTask;
        }
    }
}
