using System;
using System.Threading.Tasks;

using CtrDxEditor.Content;

namespace CtrDxEditor.ViewModels
{
    // Unsaved-work recovery: when a snapshot is written, cleared, and restored. The timer that calls in
    // lives on MainView; the rules live here so they can be tested without a window.
    public sealed partial class EditorViewModel
    {
        // The XML this document session last wrote to the recovery slot, or null when the slot holds
        // nothing of ours. Guards clearing, so a clean level never wipes work from an earlier session
        // or a level the user discarded.
        private string? _lastRecoveryXml;

        /// <summary>
        /// The file name a restored level came from, suggested by its first Save As. Null for any level
        /// that was not restored, and after a load, new, close or save.
        /// </summary>
        public string? RecoveredFileName { get; private set; }

        /// <summary>
        /// Writes a snapshot when the open level has unsaved edits that differ from the last snapshot this
        /// session wrote, and clears this session's snapshot once the level is back to its saved state.
        /// </summary>
        /// <param name="fileName">The level's file name for the Save As suggestion, or null when it has none.</param>
        /// <returns>True when a snapshot was written.</returns>
        public async Task<bool> TryCaptureRecoveryAsync(string? fileName)
        {
            if (Recovery is null || ToXml() is not { } xml)
            {
                return false;
            }

            if (xml == SavedBaselineXml)
            {
                if (_lastRecoveryXml is not null)
                {
                    _lastRecoveryXml = null;
                    await Recovery.ClearAsync();
                }
                return false;
            }

            if (xml == _lastRecoveryXml)
            {
                return false;
            }

            await Recovery.SaveAsync(new RecoverySnapshot
            {
                Xml = xml,
                BaselineXml = SavedBaselineXml ?? string.Empty,
                FileName = fileName,
                RopeSkin = ActiveRopeSkin,
                Background = ActiveBackground,
                CandySkin = ActiveCandySkin,
                OmNomSupport = ActiveOmNomSupport,
                SavedAt = DateTimeOffset.Now,
            });
            _lastRecoveryXml = xml;
            return true;
        }

        /// <summary>Empties the recovery slot, after a save or when the user discards a recovered level.</summary>
        public async Task ClearRecoveryAsync()
        {
            _lastRecoveryXml = null;
            if (Recovery is not null)
            {
                await Recovery.ClearAsync();
            }
        }

        /// <summary>
        /// Loads a recovered level: its XML, its pre-crash saved baseline (so it stays modified), its
        /// decoration, and its file name for Save As. Throws on unparseable XML before touching any state.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore.</param>
        public void RestoreSnapshot(RecoverySnapshot snapshot)
        {
            LoadLevelXml(snapshot.Xml);
            SavedBaselineXml = snapshot.BaselineXml;
            ActiveRopeSkin = snapshot.RopeSkin;
            ActiveBackground = snapshot.Background;
            ActiveCandySkin = snapshot.CandySkin;
            ActiveOmNomSupport = snapshot.OmNomSupport;
            RecoveredFileName = snapshot.FileName;
        }

        private void ResetRecoverySession()
        {
            _lastRecoveryXml = null;
            RecoveredFileName = null;
        }
    }
}
