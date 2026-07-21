using Avalonia.Controls;

using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.Views
{
    /// <summary>Adaptive layout: tracks the available width and switches the shell between modes.</summary>
    public partial class MainView : UserControl
    {
        private LayoutMode _layoutMode = LayoutMode.Expanded;

        /// <summary>
        /// Starts tracking the view's width and applying the matching layout mode.
        /// </summary>
        /// <remarks>
        /// Driven by the control's own bounds rather than a screen or platform query, so narrowing a
        /// desktop window exercises the compact shell exactly as a phone does.
        /// </remarks>
        private void WireLayoutMode()
        {
            PropertyChanged += (_, e) =>
            {
                if (e.Property == BoundsProperty)
                {
                    UpdateLayoutMode();
                }
            };

            UpdateLayoutMode();
        }

        /// <summary>Recomputes the mode from the current width and applies it if it changed.</summary>
        private void UpdateLayoutMode()
        {
            LayoutMode mode = AdaptiveLayout.ModeFor(Bounds.Width);
            if (mode == _layoutMode)
            {
                // Bounds change on every layout pass; reparenting panels each time would be both wasteful
                // and destructive to scroll position.
                return;
            }

            _layoutMode = mode;
            ApplyLayoutMode(mode);
        }

        /// <summary>Rearranges the shell for the given mode.</summary>
        /// <param name="mode">The layout mode to apply.</param>
        private void ApplyLayoutMode(LayoutMode mode)
        {
            // Filled in by the compact shell task; tracking lands first so mode detection can be reviewed alone.
            _ = _layoutMode;
            _ = mode;
        }
    }
}
