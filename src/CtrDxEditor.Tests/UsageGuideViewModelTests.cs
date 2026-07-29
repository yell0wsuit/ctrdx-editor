using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.UsageGuide;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests Usage Guide discovery and browser-like article history.</summary>
    public class UsageGuideViewModelTests
    {
        /// <summary>Search covers both visible copy and deliberate discovery aliases.</summary>
        /// <param name="query">Search text entered by the reader.</param>
        /// <param name="expectedId">Article identifier expected in the filtered results.</param>
        [Theory]
        [InlineData("mechanical hand", "mechanical-hands")]
        [InlineData("ctrl shift p", "keyboard-shortcuts")]
        [InlineData("magic hat", "magic-hats")]
        [InlineData("playtest", "save-export-playtest")]
        public void SearchFindsTitlesAliasesObjectsAndShortcutTerms(string query, string expectedId)
        {
            UsageGuideViewModel vm = new(UsageGuideCatalog.Articles, UsageGuideCatalog.HomeArticleId)
            {
                SearchText = query,
            };

            Assert.Contains(vm.SearchResults, article => article.Id == expectedId);
        }

        /// <summary>Search results are independent from the complete table of contents.</summary>
        [Fact]
        public void SearchDoesNotFilterArticles()
        {
            UsageGuideViewModel vm = new(UsageGuideCatalog.Articles, UsageGuideCatalog.HomeArticleId)
            {
                SearchText = "rocket",
            };

            Assert.True(vm.IsSearchActive);
            Assert.Equal(UsageGuideCatalog.Articles.Count, vm.Articles.Count);
            Assert.True(vm.SearchResults.Count < vm.Articles.Count);
        }

        /// <summary>Whitespace-only search displays the current article rather than a results page.</summary>
        [Fact]
        public void BlankSearchIsInactive()
        {
            UsageGuideViewModel vm = new(UsageGuideCatalog.Articles, UsageGuideCatalog.HomeArticleId)
            {
                SearchText = "rocket",
            };

            vm.SearchText = "  ";

            Assert.False(vm.IsSearchActive);
            Assert.True(vm.IsArticleVisible);
            Assert.Empty(vm.SearchResults);
        }

        /// <summary>Selecting a result leaves search and opens that article.</summary>
        [Fact]
        public void OpenSearchResultClearsSearchAndNavigates()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.SearchText = "second";

            vm.OpenSearchResult("second");

            Assert.Equal(string.Empty, vm.SearchText);
            Assert.False(vm.IsSearchActive);
            Assert.Equal("second", vm.SelectedArticle.Id);
            Assert.True(vm.CanGoBack);
        }

        /// <summary>The contents highlight is dropped while the search page covers the article.</summary>
        [Fact]
        public void ActiveSearchClearsTheTableOfContentsSelection()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.NavigateTo("second");

            vm.SearchText = "third";

            Assert.Equal("second", vm.SelectedArticle.Id);
            Assert.Null(vm.SelectedTocArticle);

            vm.SearchText = string.Empty;

            Assert.Same(vm.SelectedArticle, vm.SelectedTocArticle);
        }

        /// <summary>Starting and ending a search both notify the contents highlight.</summary>
        [Fact]
        public void TableOfContentsSelectionNotifiesWhenSearchTogglesAndOnNavigation()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            List<string?> changed = [];
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.SearchText = "second";
            Assert.Contains(nameof(UsageGuideViewModel.SelectedTocArticle), changed);

            changed.Clear();
            vm.SearchText = string.Empty;
            Assert.Contains(nameof(UsageGuideViewModel.SelectedTocArticle), changed);

            changed.Clear();
            vm.NavigateTo("third");
            Assert.Contains(nameof(UsageGuideViewModel.SelectedTocArticle), changed);
        }

        /// <summary>
        /// Re-selecting the article already being read leaves search without disturbing history.
        /// </summary>
        [Fact]
        public void ReselectingTheReadArticleLeavesSearchWithoutRecordingHistory()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.NavigateTo("second");
            vm.SearchText = "third";

            vm.NavigateTo(vm.SelectedArticle.Id);

            Assert.False(vm.IsSearchActive);
            Assert.True(vm.IsArticleVisible);
            Assert.Equal("second", vm.SelectedArticle.Id);
            Assert.False(vm.CanGoForward);
            Assert.Empty(vm.SearchResults);
        }

        /// <summary>Choosing a different article from the contents also leaves the search page.</summary>
        [Fact]
        public void SelectingAnotherArticleFromContentsLeavesSearch()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.SearchText = "second";

            vm.NavigateTo(vm.Articles[2].Id);

            Assert.False(vm.IsSearchActive);
            Assert.Equal("third", vm.SelectedArticle.Id);
            Assert.True(vm.CanGoBack);
        }

        /// <summary>Contents rows cover the catalog in order and outlive every navigation.</summary>
        /// <remarks>
        /// Stable row instances are the point: the highlight rides on each row's own
        /// <see cref="GuideTocRow.IsActive"/>, so rebuilding the collection would throw away the
        /// drawer's scroll position and the containers the highlight is painted on.
        /// </remarks>
        [Fact]
        public void ContentsRowsMirrorTheCatalogAndSurviveNavigation()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            GuideTocRow[] before = [.. vm.TocRows];

            vm.NavigateTo("third");
            vm.SearchText = "second";
            vm.ClearSearch();

            Assert.Equal(vm.Articles.Select(article => article.Id), vm.TocRows.Select(row => row.Id));
            Assert.Equal(before, vm.TocRows);
        }

        /// <summary>Exactly one row is active, and it is the article in the reading pane.</summary>
        [Fact]
        public void OneContentsRowIsActiveForTheArticleBeingRead()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();

            vm.NavigateTo("second");

            Assert.Equal("second", Assert.Single(vm.TocRows.Where(row => row.IsActive)).Id);
        }

        /// <summary>No row is active while the search page covers the article.</summary>
        [Fact]
        public void NoContentsRowIsActiveWhileSearching()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.NavigateTo("second");

            vm.SearchText = "third";
            Assert.DoesNotContain(vm.TocRows, row => row.IsActive);

            vm.ClearSearch();
            Assert.Equal("second", Assert.Single(vm.TocRows.Where(row => row.IsActive)).Id);
        }

        /// <summary>
        /// Jumping from a search result moves the highlight to the article that opened.
        /// </summary>
        /// <remarks>
        /// The row that switches off notifies as well as the row that switches on, which is what makes
        /// the drawer repaint correctly even though it was closed while all of this happened.
        /// </remarks>
        [Fact]
        public void JumpingFromASearchResultMovesTheActiveRow()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.NavigateTo("second");
            vm.SearchText = "third";
            List<string> notified = [];
            foreach (GuideTocRow row in vm.TocRows)
            {
                row.PropertyChanged += (sender, _) => notified.Add(((GuideTocRow)sender!).Id);
            }

            vm.OpenSearchResult("third");

            Assert.Equal("third", Assert.Single(vm.TocRows.Where(row => row.IsActive)).Id);
            Assert.Equal(["third"], notified);
        }

        /// <summary>
        /// Re-opening the article already being read still restores its highlight after search.
        /// </summary>
        [Fact]
        public void JumpingToTheArticleAlreadyOpenRestoresItsActiveRow()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.NavigateTo("second");
            vm.SearchText = "second";
            Assert.DoesNotContain(vm.TocRows, row => row.IsActive);

            vm.OpenSearchResult("second");

            Assert.Equal("second", Assert.Single(vm.TocRows.Where(row => row.IsActive)).Id);
        }

        /// <summary>Back, forward, and home all produce a visible change during a search.</summary>
        [Fact]
        public void HistoryNavigationLeavesTheSearchPage()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.NavigateTo("second");

            vm.SearchText = "third";
            vm.GoBack();
            Assert.False(vm.IsSearchActive);
            Assert.Equal("home", vm.SelectedArticle.Id);

            vm.SearchText = "third";
            vm.GoForward();
            Assert.False(vm.IsSearchActive);
            Assert.Equal("second", vm.SelectedArticle.Id);

            vm.SearchText = "third";
            vm.GoHome();
            Assert.False(vm.IsSearchActive);
            Assert.Equal("home", vm.SelectedArticle.Id);
        }

        /// <summary>Home dismisses the search page even when the home article is already open.</summary>
        [Fact]
        public void HomeLeavesSearchWhenAlreadyOnTheHomeArticle()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.SearchText = "second";

            vm.GoHome();

            Assert.False(vm.IsSearchActive);
            Assert.Equal("home", vm.SelectedArticle.Id);
            Assert.False(vm.CanGoBack);
        }

        /// <summary>An ignored navigation has no side effect on the active search.</summary>
        [Fact]
        public void UnknownArticleIdLeavesTheSearchPageIntact()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.SearchText = "second";

            vm.NavigateTo("missing");

            Assert.True(vm.IsSearchActive);
            Assert.Equal("second", vm.SearchText);
            Assert.NotEmpty(vm.SearchResults);
        }

        /// <summary>Back and forward behave like browser article history.</summary>
        [Fact]
        public void NavigationMaintainsBackAndForwardHistory()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();

            vm.NavigateTo("second");
            vm.NavigateTo("third");
            vm.GoBack();

            Assert.Equal("second", vm.SelectedArticle.Id);
            Assert.True(vm.CanGoBack);
            Assert.True(vm.CanGoForward);

            vm.GoForward();

            Assert.Equal("third", vm.SelectedArticle.Id);
            Assert.False(vm.CanGoForward);
        }

        /// <summary>A new branch of navigation discards stale forward history.</summary>
        [Fact]
        public void NewNavigationClearsForwardHistory()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.NavigateTo("second");
            vm.NavigateTo("third");
            vm.GoBack();

            vm.NavigateTo("home");

            Assert.Equal("home", vm.SelectedArticle.Id);
            Assert.False(vm.CanGoForward);
        }

        /// <summary>The Home action returns to the configured welcome article.</summary>
        [Fact]
        public void HomeReturnsToConfiguredHomeArticle()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();
            vm.NavigateTo("third");

            vm.GoHome();

            Assert.Equal("home", vm.SelectedArticle.Id);
        }

        /// <summary>Malformed related links cannot destroy current navigation state.</summary>
        [Fact]
        public void UnknownArticleIdLeavesSelectionUnchanged()
        {
            UsageGuideViewModel vm = CreateSmallViewModel();

            vm.NavigateTo("missing");

            Assert.Equal("home", vm.SelectedArticle.Id);
            Assert.False(vm.CanGoBack);
        }

        /// <summary>Related-topic identifiers resolve to article rows for the reading pane.</summary>
        [Fact]
        public void RelatedArticlesFollowTheSelectedArticle()
        {
            GuideArticle home = Article("home", "Home", ["second"]);
            UsageGuideViewModel vm = new(
                [home, Article("second", "Second"), Article("third", "Third")],
                "home");

            Assert.Collection(vm.RelatedArticles, article => Assert.Equal("second", article.Id));

            vm.NavigateTo("third");

            Assert.Empty(vm.RelatedArticles);
        }

        /// <summary>Creates a deterministic three-article navigation fixture.</summary>
        /// <returns>A view model whose home article is <c>home</c>.</returns>
        private static UsageGuideViewModel CreateSmallViewModel()
        {
            GuideArticle[] articles =
            [
                Article("home", "Home"),
                Article("second", "Second"),
                Article("third", "Third"),
            ];
            return new UsageGuideViewModel(articles, "home");
        }

        /// <summary>Creates one minimal article for navigation-only tests.</summary>
        /// <param name="id">Stable test article identifier.</param>
        /// <param name="title">Localization-key segment used to distinguish the fixture article.</param>
        /// <param name="relatedArticleIds">Optional related-topic identifiers for the fixture article.</param>
        /// <returns>A single-paragraph test article.</returns>
        private static GuideArticle Article(
            string id,
            string title,
            IReadOnlyList<string>? relatedArticleIds = null)
        {
            return new GuideArticle(
                id,
                "Guide.Section.Test",
                $"Guide.Test.{title}.Title",
                $"Guide.Test.{title}.Summary",
                $"Guide.Test.{title}.SearchTerms",
                [new GuideParagraph($"Guide.Test.{title}.Paragraph")],
                relatedArticleIds ?? []);
        }
    }
}
