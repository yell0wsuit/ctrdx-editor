using System;
using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.UsageGuide;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Guards the built-in Usage Guide as a connected, usable article catalog.</summary>
    public class UsageGuideCatalogTests
    {
        /// <summary>The catalog has a deterministic home and no ambiguous identifiers.</summary>
        [Fact]
        public void CatalogHasStableHomeAndUniqueArticleIds()
        {
            IReadOnlyList<GuideArticle> articles = UsageGuideCatalog.Articles;

            Assert.NotEmpty(articles);
            Assert.Equal("welcome", UsageGuideCatalog.HomeArticleId);
            Assert.Contains(articles, article => article.Id == UsageGuideCatalog.HomeArticleId);
            Assert.Equal(
                articles.Count,
                articles.Select(article => article.Id).Distinct(StringComparer.Ordinal).Count());
        }

        /// <summary>Every table-of-contents row opens useful copy.</summary>
        [Fact]
        public void EveryArticleHasReadableContent()
        {
            foreach (GuideArticle article in UsageGuideCatalog.Articles)
            {
                Assert.False(string.IsNullOrWhiteSpace(article.Id));
                Assert.StartsWith("Guide.", article.SectionKey, StringComparison.Ordinal);
                Assert.StartsWith("Guide.Article.", article.TitleKey, StringComparison.Ordinal);
                Assert.StartsWith("Guide.Article.", article.SummaryKey, StringComparison.Ordinal);
                Assert.False(string.IsNullOrWhiteSpace(article.Section));
                Assert.False(string.IsNullOrWhiteSpace(article.Title));
                Assert.False(string.IsNullOrWhiteSpace(article.Summary));
                Assert.NotEqual(article.SectionKey, article.Section);
                Assert.NotEqual(article.TitleKey, article.Title);
                Assert.NotEqual(article.SummaryKey, article.Summary);
                Assert.NotEmpty(article.Blocks);
            }
        }

        /// <summary>Every visible content block retains a resolvable localization key.</summary>
        [Fact]
        public void EveryContentBlockUsesLocalization()
        {
            foreach (GuideBlock block in UsageGuideCatalog.Articles.SelectMany(article => article.Blocks))
            {
                switch (block)
                {
                    case GuideParagraph paragraph:
                        AssertLocalized(paragraph.TextKey, paragraph.Text);
                        break;
                    case GuideHeading heading:
                        AssertLocalized(heading.TextKey, heading.Text);
                        break;
                    case GuideSteps steps:
                        AssertLocalized(steps.ItemsKey, string.Join('\n', steps.Items));
                        break;
                    case GuideCallout callout:
                        AssertLocalized(callout.TextKey, callout.Text);
                        break;
                    case GuideShortcutTable table:
                        Assert.All(table.Items, item =>
                        {
                            AssertLocalized(item.ActionKey, item.Action);
                            AssertLocalized(item.KeysKey, item.Keys);
                        });
                        break;
                    case GuideScreenshot screenshot:
                        AssertLocalized(screenshot.CaptionKey, screenshot.Caption);
                        break;
                    default:
                        throw new Xunit.Sdk.XunitException($"Unknown guide block: {block.GetType().Name}");
                }
            }
        }

        /// <summary>Related-topic buttons can never navigate into a dead end.</summary>
        [Fact]
        public void EveryRelatedTopicTargetsAnExistingArticle()
        {
            HashSet<string> ids = UsageGuideCatalog.Articles
                .Select(article => article.Id)
                .ToHashSet(StringComparer.Ordinal);

            foreach (GuideArticle article in UsageGuideCatalog.Articles)
            {
                foreach (string relatedId in article.RelatedArticleIds)
                {
                    Assert.Contains(relatedId, ids);
                }
            }
        }

        /// <summary>Content authors have named slots that can be filled with screenshots later.</summary>
        [Fact]
        public void CatalogIncludesReplaceableScreenshotSlots()
        {
            GuideScreenshot[] screenshots =
            [
                .. UsageGuideCatalog.Articles
                .SelectMany(article => article.Blocks)
                .OfType<GuideScreenshot>(),
            ];

            Assert.NotEmpty(screenshots);
            Assert.All(
                screenshots,
                screenshot => Assert.EndsWith(".png", screenshot.SuggestedFileName, StringComparison.OrdinalIgnoreCase));
            Assert.Contains(screenshots, screenshot => string.IsNullOrWhiteSpace(screenshot.Source));
        }

        /// <summary>Asserts that a guide key resolves to meaningful localized copy.</summary>
        /// <param name="key">Localization key retained by the guide model.</param>
        /// <param name="value">Display value resolved through <c>Localizer</c>.</param>
        private static void AssertLocalized(string key, string value)
        {
            Assert.StartsWith("Guide.", key, StringComparison.Ordinal);
            Assert.NotEqual(key, value);
            Assert.False(string.IsNullOrWhiteSpace(value));
        }
    }
}
