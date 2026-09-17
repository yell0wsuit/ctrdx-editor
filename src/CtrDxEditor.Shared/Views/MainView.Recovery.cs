using System;

using Avalonia.Threading;

using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    // Drives unsaved-work capture. A tick rather than an edit event, because canvas drags change the
    // document without raising ObjectMutated or touching undo; the view model writes only on change.
    public partial class MainView
    {
        private readonly DispatcherTimer _recoveryTimer = new() { Interval = TimeSpan.FromSeconds(2) };
        private bool _recoveryCaptureRunning;

        /// <summary>
        /// Starts periodic snapshotting of unsaved work. Called once startup has resolved any recovered
        /// snapshot, so capture can never overwrite work the user has not yet been offered.
        /// </summary>
        public void StartRecoveryCapture()
        {
            _recoveryTimer.Tick -= RecoveryTimer_Tick;
            _recoveryTimer.Tick += RecoveryTimer_Tick;
            _recoveryTimer.Start();
        }

        private async void RecoveryTimer_Tick(object? sender, EventArgs e)
        {
            // A slow store (IndexedDB) can outlast a tick; skip rather than overlap writes.
            if (_recoveryCaptureRunning || DataContext is not EditorViewModel vm)
            {
                return;
            }

            _recoveryCaptureRunning = true;
            try
            {
                _ = await vm.TryCaptureRecoveryAsync(_currentLevelFile?.Name ?? vm.RecoveredFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CtrDx] Recovery snapshot failed; editing continues.\n{ex}");
            }
            finally
            {
                _recoveryCaptureRunning = false;
            }
        }
    }
}
