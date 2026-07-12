using System;

using Avalonia.Controls;
using Avalonia.Threading;

using CtrDxEditor.Rendering;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    // Live animation preview: a timer advances the elapsed clock and repaints the canvas while preview is
    // active (all objects or a single one), and stops itself when the view model turns preview off.
    public partial class MainView
    {
        private readonly DispatcherTimer _animationPreviewTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
        private DateTimeOffset _animationPreviewStartedAt;

        // Starts or stops the preview timer to match the view model, seeding the start time so the elapsed
        // clock resumes where it left off. Repaints once on stop to clear any in-progress preview frame.
        private void SyncAnimationPreviewTimer()
        {
            if (DataContext is not EditorViewModel vm || !vm.IsAnimationPreviewActive)
            {
                _animationPreviewTimer.Stop();
                this.FindControl<LevelCanvas>("Canvas")!.InvalidateVisual();
                return;
            }

            _animationPreviewStartedAt = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(vm.AnimationPreviewElapsedSeconds);
            if (!_animationPreviewTimer.IsEnabled)
            {
                _animationPreviewTimer.Start();
            }
        }

        private void AnimationPreviewTimer_Tick(object? sender, EventArgs e)
        {
            if (DataContext is not EditorViewModel { IsAnimationPreviewActive: true } vm)
            {
                _animationPreviewTimer.Stop();
                return;
            }

            vm.AnimationPreviewElapsedSeconds = (DateTimeOffset.UtcNow - _animationPreviewStartedAt).TotalSeconds;
            this.FindControl<LevelCanvas>("Canvas")!.InvalidateVisual();
        }
    }
}
