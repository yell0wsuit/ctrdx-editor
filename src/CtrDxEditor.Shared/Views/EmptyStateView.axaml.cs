using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace CtrDxEditor.Views
{
    /// <summary>Shown over the canvas while no document is open.</summary>
    /// <remarks>
    /// Talks to its host through callbacks rather than by resolving <c>MainView</c> from the visual tree,
    /// matching how <c>LevelCanvas</c> is already wired. That keeps the control testable on its own and
    /// stops it depending on where it happens to be parented.
    /// </remarks>
    public partial class EmptyStateView : UserControl
    {
        /// <summary>Whether the drag-and-drop hint is shown.</summary>
        public static readonly StyledProperty<bool> ShowDropHintProperty =
            AvaloniaProperty.Register<EmptyStateView, bool>(nameof(ShowDropHint), defaultValue: true);

        /// <summary>Creates the empty state.</summary>
        public EmptyStateView()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>Whether the drag-and-drop hint is shown.</summary>
        /// <remarks>
        /// Set by the host from the layout mode: dropping needs a file to drag from, which a phone does
        /// not have. Gated on layout rather than a platform capability because layout already governs
        /// every other touch affordance, and one line of hint text does not justify new plumbing.
        /// </remarks>
        public bool ShowDropHint
        {
            get => GetValue(ShowDropHintProperty);
            set => SetValue(ShowDropHintProperty, value);
        }

        /// <summary>Raised when the New Level button is pressed.</summary>
        public Action? NewRequested { get; set; }

        /// <summary>Raised when the Open Level button is pressed.</summary>
        public Action? OpenRequested { get; set; }

        private void New_Click(object? sender, RoutedEventArgs e)
        {
            NewRequested?.Invoke();
        }

        private void Open_Click(object? sender, RoutedEventArgs e)
        {
            OpenRequested?.Invoke();
        }
    }
}
