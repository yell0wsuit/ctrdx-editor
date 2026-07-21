using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the width breakpoint that selects the compact or expanded editor layout.</summary>
    public class AdaptiveLayoutTests
    {
        /// <summary>Phone and small-tablet widths use the compact shell.</summary>
        [Theory]
        [InlineData(320)]
        [InlineData(390)]
        [InlineData(414)]
        [InlineData(768)]
        [InlineData(1023)]
        public void NarrowWidthsAreCompact(double width)
        {
            Assert.Equal(LayoutMode.Compact, AdaptiveLayout.ModeFor(width));
        }

        /// <summary>Desktop and landscape-tablet widths keep the three-column layout.</summary>
        [Theory]
        [InlineData(1024)]
        [InlineData(1280)]
        [InlineData(1920)]
        [InlineData(3840)]
        public void WideWidthsAreExpanded(double width)
        {
            Assert.Equal(LayoutMode.Expanded, AdaptiveLayout.ModeFor(width));
        }

        /// <summary>The boundary is inclusive at 1024, matching a CSS min-width media query.</summary>
        [Fact]
        public void BoundaryIsInclusiveAtBreakpoint()
        {
            Assert.Equal(LayoutMode.Compact, AdaptiveLayout.ModeFor(AdaptiveLayout.CompactMaxWidth - 0.01));
            Assert.Equal(LayoutMode.Expanded, AdaptiveLayout.ModeFor(AdaptiveLayout.CompactMaxWidth));
        }

        /// <summary>The breakpoint is Tailwind's lg, chosen so the canvas holds the majority of the width.</summary>
        [Fact]
        public void BreakpointIsTenTwentyFour()
        {
            Assert.Equal(1024, AdaptiveLayout.CompactMaxWidth);
        }

        /// <summary>
        /// Degenerate widths are compact. A control reports zero bounds before its first layout pass, and
        /// treating that as expanded would flash the three-column layout on a phone at startup.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(double.NaN)]
        public void DegenerateWidthsAreCompact(double width)
        {
            Assert.Equal(LayoutMode.Compact, AdaptiveLayout.ModeFor(width));
        }
    }
}
