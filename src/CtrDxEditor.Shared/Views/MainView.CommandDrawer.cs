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
    /// <summary>The compact command drawer: native SplitView state and the inputs that drive it.</summary>
    public partial class MainView : UserControl
    {
        private bool _restoreCommandDrawerFocusOnClose = true;

        /// <summary>Whether the command drawer is currently showing.</summary>
        private bool IsCommandDrawerOpen =>
            this.FindControl<SplitView>("CompactCommandDrawer") is { IsPaneOpen: true };

        /// <summary>
        /// The single authority over programmatic drawer visibility.
        /// </summary>
        /// <param name="open">Whether the drawer should be showing.</param>
        /// <param name="restoreFocus">Whether closing returns focus to the hamburger.</param>
        /// <remarks>
        /// Programmatic dismissal paths route here rather than setting pane state themselves. Native
        /// light-dismiss is synchronized by <see cref="CompactCommandDrawer_PaneClosed"/>. Opening is
        /// refused outside compact mode: a stale open flag surviving a resize would leave the drawer
        /// covering the expanded three-column layout.
        /// </remarks>
        private void SetCommandDrawerOpen(bool open, bool restoreFocus = true)
        {
            if (_layoutMode != LayoutMode.Compact)
            {
                open = false;
            }

            if (this.FindControl<SplitView>("CompactCommandDrawer") is not { } drawer
                || this.FindControl<Button>("CompactMenuButton") is not { } button)
            {
                return;
            }

            _restoreCommandDrawerFocusOnClose = restoreFocus;
            drawer.IsHitTestVisible = open;
            drawer.IsPaneOpen = open;
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
            if (this.FindControl<SplitView>("CompactCommandDrawer") is not { IsPaneOpen: true } drawer)
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

        /// <summary>Synchronizes editor chrome after SplitView's native light-dismiss closes the pane.</summary>
        private void CompactCommandDrawer_PaneClosed(object? sender, RoutedEventArgs e)
        {
            if (sender is SplitView drawer
                && this.FindControl<Button>("CompactMenuButton") is { } button)
            {
                drawer.IsHitTestVisible = false;
                Avalonia.Automation.AutomationProperties.SetName(
                    button,
                    Localizer.Get("CommandDrawer.Open"));
                if (_restoreCommandDrawerFocusOnClose)
                {
                    _ = button.Focus();
                }

                UpdateCompactEditBarVisibility();
            }
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

        /// <summary>Closes the drawer, then runs a one-shot command.</summary>
        /// <param name="run">The command to run once the drawer is down.</param>
        /// <remarks>
        /// Closing first matters because most of these open a dialog or a file picker: leaving the drawer
        /// up would put it over the thing the user is being asked to answer. Toggles deliberately do not
        /// use this - their effect is on the canvas behind the drawer, so closing would hide the result.
        /// </remarks>
        private void CloseDrawerThen(System.Action run)
        {
            SetCommandDrawerOpen(false);
            run();
        }

        private void DrawerSelectAll_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => SelectAll_Click(sender, e));
        }

        private void DrawerClearClipboard_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => ClearClipboard_Click(sender, e));
        }

        private void DrawerLevelSettings_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => LevelSettings_Click(sender, e));
        }

        private void DrawerZoomIn_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => ZoomIn_Click(sender, e));
        }

        private void DrawerZoomOut_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => ZoomOut_Click(sender, e));
        }

        // The four view toggles and the animation preview keep the drawer open: each one is flip-and-look,
        // and what the user needs to look at is the canvas the drawer is covering.
        private void DrawerAnimationPreview_Click(object? sender, RoutedEventArgs e)
        {
            AnimationPreviewToggle_Click(sender, e);
        }

        private void DrawerSnapToggle_Click(object? sender, RoutedEventArgs e)
        {
            SnapToggle_Click(sender, e);
        }

        private void DrawerShowHitboxes_Click(object? sender, RoutedEventArgs e)
        {
            ShowHitboxesToggle_Click(sender, e);
        }

        private void DrawerShowForceFields_Click(object? sender, RoutedEventArgs e)
        {
            ShowForceFieldsToggle_Click(sender, e);
        }

        private void DrawerShowMovementPaths_Click(object? sender, RoutedEventArgs e)
        {
            ShowMovementPathsToggle_Click(sender, e);
        }

        private void DrawerNew_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => New_Click(sender, e));
        }

        private void DrawerOpen_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => Open_Click(sender, e));
        }

        private void DrawerSave_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => Save_Click(sender, e));
        }

        private void DrawerSaveAs_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => SaveAs_Click(sender, e));
        }

        private void DrawerScreenshot_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => Screenshot_Click(sender, e));
        }

        private void DrawerReviewChanges_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => ReviewChanges_Click(sender, e));
        }

        private void DrawerPlaytest_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => Playtest_Click(sender, e));
        }

        private void DrawerSetDxLocation_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => SetDxLocation_Click(sender, e));
        }

        private void DrawerClose_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => Close_Click(sender, e));
        }

        /// <summary>Closes the compact command drawer and opens the Usage Guide.</summary>
        /// <param name="sender">Drawer row that raised the event.</param>
        /// <param name="e">Click event data forwarded to the shared command.</param>
        private void DrawerUsageGuide_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => UsageGuide_Click(sender, e));
        }

        /// <summary>Closes the compact command drawer and opens About on supported platforms.</summary>
        /// <param name="sender">Drawer row that raised the event.</param>
        /// <param name="e">Click event data forwarded to the shared command.</param>
        private void DrawerAbout_Click(object? sender, RoutedEventArgs e)
        {
            CloseDrawerThen(() => About_Click(sender, e));
        }
    }
}
