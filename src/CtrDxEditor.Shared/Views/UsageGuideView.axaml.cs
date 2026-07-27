using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

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

        /// <summary>Synchronizes article selection and scroll position after navigation.</summary>
        /// <param name="sender">Guide view model raising the notification.</param>
        /// <param name="e">Name of the changed view-model property.</param>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UsageGuideViewModel.SelectedArticle))
            {
                this.FindControl<ListBox>("TableOfContents")!.SelectedItem = _viewModel.SelectedArticle;
                ScrollArticleToTop();
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
            Avalonia.Automation.AutomationProperties.SetName(
                sidebarButton,
                Localizer.Get("Guide.Navigation.ShowContents"));
            ApplyToolbarLayout(searchBox);
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

        /// <summary>Navigates to the selected table-of-contents article.</summary>
        /// <param name="sender">Table-of-contents list whose selection changed.</param>
        /// <param name="e">Added and removed list selections.</param>
        private void TableOfContents_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox { SelectedItem: GuideArticle article })
            {
                _viewModel.NavigateTo(article.Id);
                CloseCompactSidebar();
            }
        }

        /// <summary>Leaves search and opens the selected result article.</summary>
        /// <param name="sender">Dedicated search-results list whose selection changed.</param>
        /// <param name="e">Added and removed result selections.</param>
        private void SearchResults_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox { SelectedItem: GuideSearchResult result })
            {
                _viewModel.OpenSearchResult(result.Id);
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

        /// <summary>Synchronizes the hamburger label after native light-dismiss closes the drawer.</summary>
        /// <param name="sender">Split view whose pane closed.</param>
        /// <param name="e">Pane-closed event data.</param>
        private void GuideSplitView_PaneClosed(object? sender, RoutedEventArgs e)
        {
            if (this.FindControl<Button>("SidebarButton") is { } button)
            {
                Avalonia.Automation.AutomationProperties.SetName(
                    button,
                    Localizer.Get("Guide.Navigation.ShowContents"));
            }
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

        /// <summary>Returns the reading pane to the start of the newly selected article.</summary>
        private void ScrollArticleToTop()
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
