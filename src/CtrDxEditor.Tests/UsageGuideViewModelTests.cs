using System.Collections.Generic;

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
