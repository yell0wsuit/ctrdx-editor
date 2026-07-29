using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using CtrDxEditor.UsageGuide;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Search and browser-like navigation state for the built-in Usage Guide.</summary>
    /// <remarks>
    /// Four fields hold every piece of guide state: the selected article, the raw search text, and
    /// the two history stacks. Everything a binding can see is computed from them on demand, and
    /// all mutations funnel through <see cref="Mutate"/>, which diffs a snapshot taken before and
    /// after the change and raises exactly the notifications that diff justifies. No presentation
    /// value is cached, so none can survive the state that produced it.
    /// </remarks>
    public sealed class UsageGuideViewModel : INotifyPropertyChanged
    {
        private readonly Dictionary<string, GuideArticle> _byId;
        private readonly string _homeArticleId;
        private readonly Stack<GuideArticle> _back = [];
        private readonly Stack<GuideArticle> _forward = [];

        private string _searchText = string.Empty;

        /// <summary>Creates guide state over an article catalog.</summary>
        /// <param name="articles">Complete ordered article catalog.</param>
        /// <param name="homeArticleId">Stable identifier selected initially and by <see cref="GoHome"/>.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="articles"/> is empty or does not contain
        /// <paramref name="homeArticleId"/>.
        /// </exception>
        public UsageGuideViewModel(IReadOnlyList<GuideArticle> articles, string homeArticleId)
        {
            if (articles.Count == 0)
            {
                throw new ArgumentException("The Usage Guide requires at least one article.", nameof(articles));
            }

            Articles = articles;
            _byId = articles.ToDictionary(article => article.Id, StringComparer.Ordinal);
            if (!_byId.TryGetValue(homeArticleId, out GuideArticle? home))
            {
                throw new ArgumentException("The home article must exist in the catalog.", nameof(homeArticleId));
            }

            _homeArticleId = homeArticleId;
            SelectedArticle = home;
            TocRows = [.. articles.Select(article => new GuideTocRow(article))];
            SyncTocRows();
        }

        /// <summary>Raised when presentation state changes.</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>The complete table of contents.</summary>
        public IReadOnlyList<GuideArticle> Articles { get; }

        /// <summary>The contents rows the drawer displays, one per article, in catalog order.</summary>
        /// <remarks>
        /// Created once and never replaced. Each row owns the highlight for its article, so the drawer
        /// keeps its scroll position across navigation and the highlight is a property of the data rather
        /// than of whichever rows a list happens to have realized.
        /// </remarks>
        public IReadOnlyList<GuideTocRow> TocRows { get; }

        /// <summary>Article cards matching the active <see cref="SearchText"/>.</summary>
        public ObservableCollection<GuideSearchResult> SearchResults { get; } = [];

        /// <summary>The article shown in the reading pane.</summary>
        /// <remarks>
        /// The setter is deliberately notification-free: <see cref="Publish"/> owns every change
        /// notification so that a mutation touching several fields reports them as one coherent step.
        /// </remarks>
        public GuideArticle SelectedArticle { get; private set; }

        /// <summary>
        /// The table-of-contents row the reader is actually looking at, or <see langword="null"/>
        /// while the search page covers the reading pane.
        /// </summary>
        /// <remarks>
        /// Read-only, and the single source the row flags are derived from: the contents list activates
        /// rows by click like every other guide destination, so nothing writes the highlight back and it
        /// cannot disagree with the visible pane. Reporting <see langword="null"/> during search is what
        /// drops the highlight while the results page covers the article.
        /// </remarks>
        public GuideArticle? SelectedTocArticle => IsSearchActive ? null : SelectedArticle;

        /// <summary>Text used to populate the dedicated search-results page.</summary>
        public string SearchText
        {
            get => _searchText;
            set => Mutate(() => _searchText = value ?? string.Empty);
        }

        /// <summary>Whether the dedicated search-results page is visible.</summary>
        public bool IsSearchActive => Query.Length > 0;

        /// <summary>Whether the selected article is visible instead of search results.</summary>
        public bool IsArticleVisible => !IsSearchActive;

        /// <summary>Whether the active search has at least one result.</summary>
        public bool HasSearchResults => SearchResults.Count > 0;

        /// <summary>Whether the active search has no results.</summary>
        public bool HasNoSearchResults => IsSearchActive && !HasSearchResults;

        /// <summary>Whether a previously visited article is available.</summary>
        public bool CanGoBack => _back.Count > 0;

        /// <summary>Whether an article left by going back is available.</summary>
        public bool CanGoForward => _forward.Count > 0;

        /// <summary>Resolved articles offered as related topics for <see cref="SelectedArticle"/>.</summary>
        public IReadOnlyList<GuideArticle> RelatedArticles =>
            [.. SelectedArticle.RelatedArticleIds
                .Where(_byId.ContainsKey)
                .Select(id => _byId[id])];

        /// <summary>Whether the selected article has at least one valid related-topic destination.</summary>
        public bool HasRelatedArticles => RelatedArticles.Count > 0;

        /// <summary>The trimmed query driving <see cref="SearchResults"/> and search visibility.</summary>
        private string Query => _searchText.Trim();

        /// <summary>Navigates to an article by stable identifier, leaving the search page.</summary>
        /// <param name="articleId">Destination identifier; unknown identifiers are ignored entirely.</param>
        public void NavigateTo(string articleId)
        {
            Mutate(() => Open(articleId));
        }

        /// <summary>Returns to the previous article, leaving the search page.</summary>
        public void GoBack()
        {
            Mutate(() =>
            {
                if (_back.TryPop(out GuideArticle? destination))
                {
                    _searchText = string.Empty;
                    _forward.Push(SelectedArticle);
                    SelectedArticle = destination;
                }
            });
        }

        /// <summary>Revisits the article most recently left by going back, leaving the search page.</summary>
        public void GoForward()
        {
            Mutate(() =>
            {
                if (_forward.TryPop(out GuideArticle? destination))
                {
                    _searchText = string.Empty;
                    _back.Push(SelectedArticle);
                    SelectedArticle = destination;
                }
            });
        }

        /// <summary>Navigates to the configured welcome article, leaving the search page.</summary>
        public void GoHome()
        {
            NavigateTo(_homeArticleId);
        }

        /// <summary>Leaves search and opens one of its article results.</summary>
        /// <param name="articleId">Stable identifier carried by the selected result.</param>
        public void OpenSearchResult(string articleId)
        {
            NavigateTo(articleId);
        }

        /// <summary>Dismisses the search page and restores the article underneath it.</summary>
        public void ClearSearch()
        {
            Mutate(() => _searchText = string.Empty);
        }

        /// <summary>
        /// Applies a state change, refreshes results when the query moved, and notifies every
        /// binding whose value the change could have altered.
        /// </summary>
        /// <param name="apply">Mutation acting directly on the backing state fields.</param>
        private void Mutate(Action apply)
        {
            GuideState before = Capture();
            apply();
            if (!string.Equals(Query, before.Query, StringComparison.Ordinal))
            {
                RebuildSearchResults();
            }

            Publish(before, Capture());
        }

        /// <summary>
        /// Selects an article and leaves search, without recording history for an unknown
        /// destination or for the article already being read.
        /// </summary>
        /// <param name="articleId">Destination identifier.</param>
        private void Open(string articleId)
        {
            if (!_byId.TryGetValue(articleId, out GuideArticle? destination))
            {
                return;
            }

            // Reaching the current article still counts as leaving search: the reader asked to see
            // that article, and it is behind the results page.
            _searchText = string.Empty;
            if (ReferenceEquals(destination, SelectedArticle))
            {
                return;
            }

            _back.Push(SelectedArticle);
            _forward.Clear();
            SelectedArticle = destination;
        }

        /// <summary>
        /// Repopulates <see cref="SearchResults"/> from the trimmed query across every localized
        /// searchable article field.
        /// </summary>
        private void RebuildSearchResults()
        {
            SearchResults.Clear();
            string query = Query;
            if (query.Length == 0)
            {
                return;
            }

            foreach (GuideArticle article in Articles.Where(article => Matches(article, query)))
            {
                SearchResults.Add(new GuideSearchResult(article, query));
            }
        }

        /// <summary>Records the state every derived presentation value is computed from.</summary>
        /// <returns>A snapshot comparable against a later one to detect observable change.</returns>
        private GuideState Capture()
        {
            return new GuideState(
                SelectedArticle,
                _searchText,
                Query,
                SearchResults.Count,
                _back.Count,
                _forward.Count);
        }

        /// <summary>Raises change notifications for every derived value the mutation moved.</summary>
        /// <param name="before">State captured before the mutation.</param>
        /// <param name="after">State captured after the mutation and any result rebuild.</param>
        private void Publish(in GuideState before, in GuideState after)
        {
            bool articleChanged = !ReferenceEquals(before.Article, after.Article);
            bool searchActiveChanged = (before.Query.Length > 0) != (after.Query.Length > 0);

            if (articleChanged)
            {
                OnPropertyChanged(nameof(SelectedArticle));
                OnPropertyChanged(nameof(RelatedArticles));
                OnPropertyChanged(nameof(HasRelatedArticles));
            }

            if (!string.Equals(before.SearchText, after.SearchText, StringComparison.Ordinal))
            {
                OnPropertyChanged(nameof(SearchText));
            }

            if (searchActiveChanged)
            {
                OnPropertyChanged(nameof(IsSearchActive));
                OnPropertyChanged(nameof(IsArticleVisible));
            }

            if (articleChanged || searchActiveChanged)
            {
                SyncTocRows();
                OnPropertyChanged(nameof(SelectedTocArticle));
            }

            if (before.ResultCount != after.ResultCount || searchActiveChanged)
            {
                OnPropertyChanged(nameof(HasSearchResults));
                OnPropertyChanged(nameof(HasNoSearchResults));
            }

            if (before.BackCount != after.BackCount)
            {
                OnPropertyChanged(nameof(CanGoBack));
            }

            if (before.ForwardCount != after.ForwardCount)
            {
                OnPropertyChanged(nameof(CanGoForward));
            }
        }

        /// <summary>Points every contents row's highlight at <see cref="SelectedTocArticle"/>.</summary>
        /// <remarks>
        /// Each row raises its own change notification, so the row losing the highlight repaints as
        /// surely as the row gaining it - including rows in a drawer that was closed throughout.
        /// </remarks>
        private void SyncTocRows()
        {
            GuideArticle? active = SelectedTocArticle;
            foreach (GuideTocRow row in TocRows)
            {
                row.SetActive(ReferenceEquals(row.Article, active));
            }
        }

        /// <summary>Tests one article against the query across all of its searchable text.</summary>
        /// <param name="article">Catalog article to inspect.</param>
        /// <param name="query">Non-empty trimmed search query.</param>
        /// <returns><see langword="true"/> when any searchable field contains the query.</returns>
        private static bool Matches(GuideArticle article, string query)
        {
            return Contains(article.Title, query)
                || Contains(article.Section, query)
                || Contains(article.Summary, query)
                || article.SearchTerms.Any(term => Contains(term, query))
                || article.Blocks.OfType<GuideParagraph>().Any(block => Contains(block.Text, query))
                || article.Blocks.OfType<GuideHeading>().Any(block => Contains(block.Text, query))
                || article.Blocks.OfType<GuideShortcutTable>().Any(
                    table => table.Items.Any(
                        shortcut => Contains(shortcut.Action, query) || Contains(shortcut.Keys, query)));
        }

        /// <summary>Tests a localized value for an ordinal, case-insensitive query match.</summary>
        /// <param name="value">Localized text to inspect.</param>
        /// <param name="query">Non-empty trimmed search query.</param>
        /// <returns><see langword="true"/> when <paramref name="query"/> occurs in <paramref name="value"/>.</returns>
        private static bool Contains(string value, string query)
        {
            return value.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Raises <see cref="PropertyChanged"/> for a changed presentation property.</summary>
        /// <param name="propertyName">
        /// Changed property name, supplied automatically by the compiler when omitted.
        /// </param>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>An immutable reading of the state behind every derived guide property.</summary>
        /// <param name="Article">Article shown in the reading pane.</param>
        /// <param name="SearchText">Raw search-box text, including untrimmed whitespace.</param>
        /// <param name="Query">Trimmed query driving results and search visibility.</param>
        /// <param name="ResultCount">Number of populated search-result cards.</param>
        /// <param name="BackCount">Depth of the backward history stack.</param>
        /// <param name="ForwardCount">Depth of the forward history stack.</param>
        private readonly record struct GuideState(
            GuideArticle Article,
            string SearchText,
            string Query,
            int ResultCount,
            int BackCount,
            int ForwardCount);
    }

    /// <summary>One table-of-contents row, carrying its own highlight state.</summary>
    /// <param name="article">Article this row navigates to.</param>
    public sealed class GuideTocRow(GuideArticle article) : INotifyPropertyChanged
    {
        /// <summary>Raised when the row's highlight turns on or off.</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>The article this row stands for.</summary>
        public GuideArticle Article { get; } = article;

        /// <summary>Stable article identifier carried to navigation.</summary>
        public string Id => Article.Id;

        /// <summary>Localized table-of-contents section.</summary>
        public string Section => Article.Section;

        /// <summary>Localized article title.</summary>
        public string Title => Article.Title;

        /// <summary>Localized article summary.</summary>
        public string Summary => Article.Summary;

        /// <summary>Whether this row is the one being read, and so carries the highlight.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Turns the highlight on or off, notifying only on a real change.</summary>
        /// <param name="active">Whether this row is the article in the reading pane.</param>
        internal void SetActive(bool active)
        {
            if (IsActive == active)
            {
                return;
            }

            IsActive = active;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
        }
    }

    /// <summary>One article card displayed on the dedicated Usage Guide search page.</summary>
    /// <param name="Article">Matching article opened when the card is selected.</param>
    /// <param name="Query">Trimmed query highlighted in the card's visible metadata.</param>
    public sealed record GuideSearchResult(GuideArticle Article, string Query)
    {
        /// <summary>Stable article identifier used by navigation.</summary>
        public string Id => Article.Id;

        /// <summary>Localized table-of-contents section.</summary>
        public string Section => Article.Section;

        /// <summary>Localized article title.</summary>
        public string Title => Article.Title;

        /// <summary>Localized article summary.</summary>
        public string Summary => Article.Summary;
    }
}
