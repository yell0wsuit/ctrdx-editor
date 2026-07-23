using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;

namespace CtrDxEditor.Views
{
    /// <summary>The compact command drawer: one open/close path and the inputs that drive it.</summary>
    public partial class MainView : UserControl
    {
        /// <summary>Whether the command drawer is currently showing.</summary>
        private bool IsCommandDrawerOpen =>
            this.FindControl<Border>("CompactCommandDrawer") is { IsVisible: true };

        /// <summary>
        /// The single authority over drawer visibility.
        /// </summary>
        /// <param name="open">Whether the drawer should be showing.</param>
        /// <param name="restoreFocus">Whether closing returns focus to the hamburger.</param>
        /// <remarks>
        /// Every dismissal path routes here rather than setting visibility itself, so the drawer, the
        /// scrim and the hamburger's automation name cannot end up in three different states. Opening is
        /// refused outside compact mode: a stale open flag surviving a resize would leave the drawer
        /// covering the expanded three-column layout.
        /// </remarks>
        private void SetCommandDrawerOpen(bool open, bool restoreFocus = true)
        {
            if (_layoutMode != LayoutMode.Compact)
            {
                open = false;
            }

            if (this.FindControl<Border>("CompactCommandDrawer") is not { } drawer
                || this.FindControl<Border>("CompactCommandScrim") is not { } scrim
                || this.FindControl<Button>("CompactMenuButton") is not { } button)
            {
                return;
            }

            drawer.IsVisible = open;
            scrim.IsVisible = open;
            Avalonia.Automation.AutomationProperties.SetName(
                button,
                Localizer.Get(open ? "CommandDrawer.Close" : "CommandDrawer.Open"));

            if (open)
            {
                Dispatcher.UIThread.Post(FocusFirstCommandRow, DispatcherPriority.Loaded);
            }
            else if (restoreFocus)
            {
                _ = button.Focus();
            }

            UpdateCompactEditBarVisibility();
        }

        /// <summary>Moves focus to the first row a keyboard user can actually reach.</summary>
        private void FocusFirstCommandRow()
        {
            if (this.FindControl<Border>("CompactCommandDrawer") is not { IsVisible: true } drawer)
            {
                return;
            }

            Button? first = drawer.GetVisualDescendants()
                .OfType<Button>()
                .FirstOrDefault(b => b is { IsVisible: true, IsEffectivelyEnabled: true });
            _ = first?.Focus();
        }

        /// <summary>Toggles the drawer from the hamburger.</summary>
        private void CompactMenuButton_Click(object? sender, RoutedEventArgs e)
        {
            SetCommandDrawerOpen(!IsCommandDrawerOpen);
        }

        /// <summary>Closes the drawer when the scrim is pressed.</summary>
        private void CompactCommandScrim_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            SetCommandDrawerOpen(false);
            e.Handled = true;
        }

        /// <summary>
        /// Closes the drawer on Escape.
        /// </summary>
        /// <remarks>
        /// Escape only. Android's back button reaches a browser app as a history <c>popstate</c>, not a
        /// key event, and iOS has no back key at all, so wiring a back-key enum member would add coverage
        /// for behaviour that never fires on the platforms this shell targets.
        /// </remarks>
        private void CompactCommandDrawer_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetCommandDrawerOpen(false);
                e.Handled = true;
            }
        }
    }
}
