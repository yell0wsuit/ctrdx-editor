using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using CtrDxEditor.Localization;
using CtrDxEditor.UsageGuide;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>Adaptive, searchable reading surface shared by desktop and browser guide hosts.</summary>
    public partial class UsageGuideView : UserControl
    {
        private readonly UsageGuideViewModel _viewModel;
        private bool _usesPersistentSidebar;

        /// <summary>Creates the guide surface and begins tracking its bounds and navigation state.</summary>
        public UsageGuideView()
        {
            AvaloniaXamlLoader.Load(this);
            _viewModel = new UsageGuideViewModel(
                UsageGuideCatalog.Articles,
                UsageGuideCatalog.HomeArticleId);
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            PropertyChanged += UsageGuideView_PropertyChanged;
            ApplyAdaptiveLayout();
        }

        /// <summary>Refreshes the sidebar mode whenever the guide's available bounds change.</summary>
        /// <param name="sender">Guide view raising the Avalonia property change.</param>
        /// <param name="e">Changed Avalonia property and its values.</param>
        private void UsageGuideView_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == BoundsProperty)
            {
                ApplyAdaptiveLayout();
            }
        }

        /// <summary>Returns the visible pane to its start whenever the guide shows something new.</summary>
        /// <param name="sender">Guide view model raising the notification.</param>
        /// <param name="e">Name of the changed view-model property.</param>
        /// <remarks>
        /// Table-of-contents selection is not touched here: it is bound to
        /// <see cref="UsageGuideViewModel.SelectedTocArticle"/>, and a second writer is exactly what
        /// let the highlight drift out of step with the visible pane.
        /// </remarks>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(UsageGuideViewModel.SelectedArticle)
                or nameof(UsageGuideViewModel.SearchText))
            {
                ScrollVisiblePaneToTop();
            }
        }

        /// <summary>Applies persistent-sidebar or overlay-drawer presentation for the current bounds.</summary>
        private void ApplyAdaptiveLayout()
        {
            if (this.FindControl<SplitView>("GuideSplitView") is not { } splitView
                || this.FindControl<Button>("SidebarButton") is not { } sidebarButton
                || this.FindControl<TextBox>("GuideSearchBox") is not { } searchBox)
            {
                return;
            }

            bool persistent = UsageGuideLayout.UsesPersistentSidebar(Bounds.Width, Bounds.Height);
            _usesPersistentSidebar = persistent;
            splitView.DisplayMode = persistent ? SplitViewDisplayMode.Inline : SplitViewDisplayMode.Overlay;
            splitView.IsPaneOpen = persistent;
            sidebarButton.IsVisible = !persistent;
            ApplySidebarButtonLabel(splitView.IsPaneOpen);
            ApplyToolbarLayout(searchBox);
        }

        /// <summary>Describes the hamburger by the action it will perform next.</summary>
        /// <param name="paneOpen">Whether the table-of-contents pane is currently open.</param>
        /// <remarks>
        /// Tooltip and accessible name are written together here so the two cannot describe
        /// different actions, and both follow <see cref="SplitView.IsPaneOpen"/> rather than being
        /// pinned to the opening half of the toggle.
        /// </remarks>
        private void ApplySidebarButtonLabel(bool paneOpen)
        {
            if (this.FindControl<Button>("SidebarButton") is not { } sidebarButton)
            {
                return;
            }

            string label = Localizer.Get(
                paneOpen ? "Guide.Navigation.HideContents" : "Guide.Navigation.ShowContents");
            Avalonia.Automation.AutomationProperties.SetName(sidebarButton, label);
            ToolTip.SetTip(sidebarButton, label);
        }

        /// <summary>Places search beside navigation or on a full-width second toolbar row.</summary>
        /// <param name="searchBox">Search field whose responsive grid placement is updated.</param>
        private void ApplyToolbarLayout(TextBox searchBox)
        {
            bool stacked = UsageGuideLayout.UsesStackedToolbar(Bounds.Width);
            Grid.SetRow(searchBox, stacked ? 1 : 0);
            Grid.SetColumn(searchBox, stacked ? 0 : 5);
            Grid.SetColumnSpan(searchBox, stacked ? 6 : 1);
            searchBox.Margin = stacked ? new Thickness(0, 8, 0, 0) : new Thickness(8, 0, 0, 0);
            searchBox.MaxWidth = stacked ? double.PositiveInfinity : 360;
        }

        /// <summary>Opens or closes the compact table-of-contents drawer.</summary>
        /// <param name="sender">Hamburger button that raised the event.</param>
        /// <param name="e">Click event data.</param>
        private void SidebarButton_Click(object? sender, RoutedEventArgs e)
        {
            if (!_usesPersistentSidebar
                && this.FindControl<SplitView>("GuideSplitView") is { } splitView)
            {
                splitView.IsPaneOpen = !splitView.IsPaneOpen;
            }
        }

        /// <summary>Returns to the previous article in guide history.</summary>
        /// <param name="sender">Back button that raised the event.</param>
        /// <param name="e">Click event data.</param>
        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            _viewModel.GoBack();
        }

        /// <summary>Revisits the next article in guide history.</summary>
        /// <param name="sender">Forward button that raised the event.</param>
        /// <param name="e">Click event data.</param>
        private void ForwardButton_Click(object? sender, RoutedEventArgs e)
        {
            _viewModel.GoForward();
        }

        /// <summary>Returns to the Usage Guide welcome article.</summary>
        /// <param name="sender">Home button that raised the event.</param>
        /// <param name="e">Click event data.</param>
        private void HomeButton_Click(object? sender, RoutedEventArgs e)
        {
            _viewModel.GoHome();
        }

        /// <summary>Closes the compact drawer after a table-of-contents row is tapped.</summary>
        /// <param name="sender">Table-of-contents list that received the pointer.</param>
        /// <param name="e">Pointer release data used to locate the tapped row.</param>
        /// <remarks>
        /// Navigation itself travels through the two-way
        /// <see cref="UsageGuideViewModel.SelectedTocArticle"/> binding. Only the drawer is handled
        /// here, and it listens to the pointer rather than to selection so that re-tapping the row
        /// already being read still dismisses the drawer.
        /// </remarks>
        private void TableOfContents_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if ((e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(includeSelf: true) is not null)
            {
                CloseCompactSidebar();
            }
        }

        /// <summary>Dismisses the search page when the reader presses Escape in the search box.</summary>
        /// <param name="sender">Guide search box that received the key.</param>
        /// <param name="e">Pressed key data.</param>
        private void GuideSearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _viewModel.ClearSearch();
                e.Handled = true;
            }
        }

        /// <summary>Leaves search and opens the chosen result article.</summary>
        /// <param name="sender">Result card carrying a stable article identifier.</param>
        /// <param name="e">Click event data.</param>
        /// <remarks>
        /// Results are activated rather than selected. A result card is a one-shot action — picking
        /// one destroys the page it lives on — so the list holds no selection that could go stale,
        /// re-entrant navigation through a rebuild is impossible, and the cards stay reachable by
        /// keyboard without an arrow key doubling as an open command.
        /// </remarks>
        private void SearchResult_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string articleId })
            {
                _viewModel.OpenSearchResult(articleId);
            }
        }

        /// <summary>Navigates to an article selected from the current article's related topics.</summary>
        /// <param name="sender">Related-topic button carrying a stable article identifier.</param>
        /// <param name="e">Click event data.</param>
        private void RelatedTopic_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string articleId })
            {
                _viewModel.NavigateTo(articleId);
            }
        }

        /// <summary>Synchronizes the hamburger label when the drawer opens.</summary>
        /// <param name="sender">Split view whose pane opened.</param>
        /// <param name="e">Pane-opened event data.</param>
        private void GuideSplitView_PaneOpened(object? sender, RoutedEventArgs e)
        {
            ApplySidebarButtonLabel(paneOpen: true);
        }

        /// <summary>Synchronizes the hamburger label after any close, including native light-dismiss.</summary>
        /// <param name="sender">Split view whose pane closed.</param>
        /// <param name="e">Pane-closed event data.</param>
        private void GuideSplitView_PaneClosed(object? sender, RoutedEventArgs e)
        {
            ApplySidebarButtonLabel(paneOpen: false);
        }

        /// <summary>Closes the table of contents after compact navigation.</summary>
        private void CloseCompactSidebar()
        {
            if (!_usesPersistentSidebar
                && this.FindControl<SplitView>("GuideSplitView") is { } splitView)
            {
                splitView.IsPaneOpen = false;
            }
        }

        /// <summary>Returns both panes to their start so no stale scroll offset survives a change.</summary>
        private void ScrollVisiblePaneToTop()
        {
            if (this.FindControl<ScrollViewer>("ArticleScroll") is { } scroll)
            {
                scroll.Offset = default;
            }

            if (this.FindControl<ScrollViewer>("SearchResultsScroll") is { } results)
            {
                results.Offset = default;
            }
        }
    }
}
