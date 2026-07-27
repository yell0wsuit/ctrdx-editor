using CtrDxEditor.Controls;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests result-only search match segmentation.</summary>
    public class SearchHighlightTextBlockTests
    {
        /// <summary>Every case-insensitive occurrence becomes a highlighted run.</summary>
        [Fact]
        public void SplitRunsHighlightsEveryCaseInsensitiveMatch()
        {
            SearchHighlightRun[] runs =
                [.. SearchHighlightTextBlock.SplitRuns("Magic hat and MAGIC rope", "magic")];

            Assert.Collection(
                runs,
                run =>
                {
                    Assert.Equal("Magic", run.Text);
                    Assert.True(run.IsMatch);
                },
                run =>
                {
                    Assert.Equal(" hat and ", run.Text);
                    Assert.False(run.IsMatch);
                },
                run =>
                {
                    Assert.Equal("MAGIC", run.Text);
                    Assert.True(run.IsMatch);
                },
                run =>
                {
                    Assert.Equal(" rope", run.Text);
                    Assert.False(run.IsMatch);
                });
        }

        /// <summary>A blank query preserves the source text as a single ordinary run.</summary>
        [Fact]
        public void SplitRunsTreatsBlankQueryAsPlainText()
        {
            SearchHighlightRun[] runs =
                [.. SearchHighlightTextBlock.SplitRuns("Magic hat", "  ")];

            SearchHighlightRun run = Assert.Single(runs);
            Assert.Equal("Magic hat", run.Text);
            Assert.False(run.IsMatch);
        }
    }
}
