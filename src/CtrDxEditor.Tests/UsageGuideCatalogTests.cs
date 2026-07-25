using System;
using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Descriptors;
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

        /// <summary>The tutorial-text article names the canvas shortcut implemented by <c>LevelCanvas</c>.</summary>
        [Fact]
        public void TutorialTextArticleDocumentsF2Shortcut()
        {
            GuideArticle article = Assert.Single(
                UsageGuideCatalog.Articles,
                candidate => candidate.Id == "tutorial-objects");
            string copy = string.Join(
                ' ',
                article.Blocks.OfType<GuideParagraph>().Select(paragraph => paragraph.Text));

            Assert.Contains("F2", copy, StringComparison.Ordinal);
            Assert.DoesNotContain("Double-click tutorial text", copy, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The shortcut reference includes the tutorial-text editor gesture.</summary>
        [Fact]
        public void ShortcutReferenceIncludesTutorialTextF2()
        {
            GuideArticle article = Assert.Single(
                UsageGuideCatalog.Articles,
                candidate => candidate.Id == "keyboard-shortcuts");
            GuideShortcutTable shortcuts = Assert.Single(article.Blocks.OfType<GuideShortcutTable>());

            Assert.Contains(shortcuts.Items, shortcut => shortcut.Keys.Contains("F2", StringComparison.Ordinal));
        }

        /// <summary>The bamboo-tube article describes only behavior exposed by its descriptor and canvas.</summary>
        [Fact]
        public void BambooTubeArticleDoesNotInventPairingProperties()
        {
            GuideArticle article = Assert.Single(
                UsageGuideCatalog.Articles,
                candidate => candidate.Id == "bamboo-tubes");
            string copy = string.Join(
                ' ',
                article.Blocks.OfType<GuideParagraph>().Select(paragraph => paragraph.Text));

            Assert.Contains("capture openings", copy, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pair", copy, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("configure", copy, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Maps every editable object element to the Objects-section article that documents it. Adding a
        /// descriptor without an article, or an article for an element that no longer exists, fails
        /// <see cref="EveryEditableObjectHasAnArticle"/> below.
        /// </summary>
        private static readonly Dictionary<string, string> ArticleByElement = new(StringComparer.Ordinal)
        {
            ["target"] = "objectives",
            ["candy"] = "objectives",
            ["candyL"] = "objectives",
            ["candyR"] = "objectives",
            ["star"] = "stars",
            ["grab"] = "rope-hooks",
            ["bubble"] = "bubbles",
            ["pump"] = "air-cushions",
            ["gravitySwitch"] = "gravity-buttons",
            ["bouncer1"] = "bouncers",
            ["bouncer2"] = "bouncers",
            ["spike1"] = "spikes",
            ["spike2"] = "spikes",
            ["spike3"] = "spikes",
            ["spike4"] = "spikes",
            ["electro"] = "electric-sparks",
            ["ghost"] = "ghosts",
            ["sock"] = "magic-hats",
            ["lightBulb"] = "light-bulbs",
            ["lantern"] = "lanterns",
            ["rotatedCircle"] = "vinyl",
            ["gap"] = "mice",
            ["load"] = "snails",
            ["transporter"] = "conveyors",
            ["ants"] = "ant-conveyors",
            ["pipe"] = "bamboo-tubes",
            ["steamTube"] = "steam-pipes",
            ["rocket"] = "rockets",
            ["hand"] = "mechanical-hands",
            ["tutorialText"] = "tutorial-objects",
            ["tutorial01"] = "tutorial-objects",
            ["tutorial02"] = "tutorial-objects",
            ["tutorial03"] = "tutorial-objects",
            ["tutorial04"] = "tutorial-objects",
            ["tutorial05"] = "tutorial-objects",
            ["tutorial06"] = "tutorial-objects",
            ["tutorial07"] = "tutorial-objects",
            ["tutorial08"] = "tutorial-objects",
            ["tutorial09"] = "tutorial-objects",
            ["tutorial10"] = "tutorial-objects",
            ["tutorial11"] = "tutorial-objects",
        };

        /// <summary>
        /// Every object the palette can place is documented, and every documented element still exists.
        /// This is the guard against the guide drifting away from <see cref="DescriptorTable"/>: adding an
        /// object to the descriptor table without writing its article fails here.
        /// </summary>
        [Fact]
        public void EveryEditableObjectHasAnArticle()
        {
            HashSet<string> elements = [.. DescriptorTable.CtrObjects.ByElement.Keys];
            HashSet<string> articleIds = UsageGuideCatalog.Articles
                .Select(article => article.Id)
                .ToHashSet(StringComparer.Ordinal);

            Assert.Empty(elements.Except(ArticleByElement.Keys, StringComparer.Ordinal));
            Assert.Empty(ArticleByElement.Keys.Except(elements, StringComparer.Ordinal));
            Assert.All(ArticleByElement.Values.Distinct(StringComparer.Ordinal), id => Assert.Contains(id, articleIds));
        }

        /// <summary>Every object article lives in the Objects section, so the table of contents stays coherent.</summary>
        [Fact]
        public void ObjectArticlesShareTheObjectsSection()
        {
            HashSet<string> objectArticleIds = [.. ArticleByElement.Values];

            foreach (GuideArticle article in UsageGuideCatalog.Articles.Where(a => objectArticleIds.Contains(a.Id)))
            {
                Assert.Equal("Guide.Section.GameObjects", article.SectionKey);
            }
        }

        /// <summary>Each callout exposes exactly one semantic style flag for the view.</summary>
        /// <param name="kind">Callout kind under test.</param>
        /// <param name="isTip">Expected tip-style state.</param>
        /// <param name="isNote">Expected note-style state.</param>
        /// <param name="isWarning">Expected warning-style state.</param>
        [Theory]
        [InlineData(GuideCalloutKind.Tip, true, false, false)]
        [InlineData(GuideCalloutKind.Note, false, true, false)]
        [InlineData(GuideCalloutKind.Warning, false, false, true)]
        public void CalloutKindSelectsOneSemanticStyle(
            GuideCalloutKind kind,
            bool isTip,
            bool isNote,
            bool isWarning)
        {
            GuideCallout callout = new(kind, "Guide.Article.welcome.Search.Tip");

            Assert.Equal(isTip, callout.IsTip);
            Assert.Equal(isNote, callout.IsNote);
            Assert.Equal(isWarning, callout.IsWarning);
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
