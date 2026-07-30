using System;

using Avalonia;

using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Tests badge plate layout — wrapping, clamping, and flip. Layout takes pre-measured text widths, so
    /// these run without a font manager and without depending on how wide any particular string renders.
    /// </summary>
    public class BadgeLayoutTests
    {
        private static readonly Size Wide = new(1600, 900);
        private static readonly Size Narrow = new(400, 700);

        /// <summary>Width of the inline separator, standing in for a measured middot.</summary>
        private const double Sep = 4;

        /// <summary>A short entry: a one-letter label and a three-digit value.</summary>
        private static readonly BadgeRenderer.EntryMetrics Small = new(8, 20);

        /// <summary>A mid-length entry, near the cap on a desktop canvas and over it on a phone.</summary>
        private static readonly BadgeRenderer.EntryMetrics Medium = new(60, 20);

        /// <summary>A long entry, well past the cap at any canvas size.</summary>
        private static readonly BadgeRenderer.EntryMetrics Large = new(200, 20);

        private static BadgeRenderer.Layout Measure(
            BadgeRenderer.EntryMetrics[] entries, Point anchor, Size bounds)
        {
            return BadgeRenderer.Measure(entries, Sep, anchor, bounds);
        }

        /// <summary>A short pair fits under the cap and stays on one row.</summary>
        [Fact]
        public void ShortPairStaysInline()
        {
            BadgeRenderer.Layout layout = Measure([Small, Small], new Point(800, 400), Wide);

            Assert.False(layout.Wrapped);
            _ = Assert.Single(layout.Rows);
        }

        /// <summary>A one-row plate measures exactly the shared badge height, so existing badges do not shift.</summary>
        [Fact]
        public void OneRowPlateMatchesTheSharedHeight()
        {
            BadgeRenderer.Layout layout = Measure([Small], new Point(800, 400), Wide);

            Assert.Equal(BadgeRenderer.Height, layout.Plate.Height, precision: 3);
        }

        /// <summary>A pair whose labels blow past the cap lays out one entry per row.</summary>
        [Fact]
        public void LongPairWraps()
        {
            BadgeRenderer.Layout layout = Measure([Large, Large], new Point(800, 400), Wide);

            Assert.True(layout.Wrapped);
            Assert.Equal(2, layout.Rows.Count);
            Assert.True(
                layout.Plate.Height > BadgeRenderer.Height,
                "A wrapped plate must be taller than a single row.");
        }

        /// <summary>A single entry never wraps, however wide it is — there is nothing to wrap it against.</summary>
        [Fact]
        public void LoneEntryNeverWraps()
        {
            BadgeRenderer.Layout layout = Measure([Large], new Point(800, 400), Narrow);

            Assert.False(layout.Wrapped);
            _ = Assert.Single(layout.Rows);
        }

        /// <summary>The plate is as wide as its widest row and no wider.</summary>
        [Fact]
        public void WrappedPlateMatchesItsWidestRow()
        {
            BadgeRenderer.Layout layout = Measure([Large, Small], new Point(800, 400), Wide);

            double widest = 0;
            foreach (Rect row in layout.Rows)
            {
                widest = Math.Max(widest, row.Width);
            }

            Assert.Equal(widest + (BadgeRenderer.Padding * 2), layout.Plate.Width, precision: 3);
        }

        /// <summary>The cap scales with the canvas, so the same pair that fits on a desktop wraps on a phone.</summary>
        [Fact]
        public void NarrowCanvasLowersTheCap()
        {
            Assert.False(Measure([Medium, Medium], new Point(800, 400), Wide).Wrapped);
            Assert.True(Measure([Medium, Medium], new Point(200, 400), Narrow).Wrapped);
        }

        /// <summary>A badge anchored at the top edge flips below rather than drawing off-canvas.</summary>
        [Fact]
        public void TopAnchorFlipsBelow()
        {
            Point anchor = new(800, 4);
            BadgeRenderer.Layout layout = Measure([Small, Small], anchor, Wide);

            Assert.True(layout.Plate.Y >= 0, "The plate must not extend above the canvas.");
            Assert.True(layout.Plate.Y > anchor.Y, "With no room above, the plate belongs below the anchor.");
        }

        /// <summary>A badge with room above keeps its default position there.</summary>
        [Fact]
        public void RoomAboveKeepsThePlateAboveTheAnchor()
        {
            Point anchor = new(800, 400);
            BadgeRenderer.Layout layout = Measure([Small, Small], anchor, Wide);

            Assert.True(layout.Plate.Bottom < anchor.Y);
        }

        /// <summary>Flip-below measures the real plate, so a tall wrapped badge still fits.</summary>
        [Fact]
        public void FlipBelowAccountsForWrappedHeight()
        {
            BadgeRenderer.Layout layout = Measure([Large, Large], new Point(800, 4), Wide);

            Assert.True(layout.Plate.Y >= 0);
            Assert.True(layout.Plate.Bottom <= Wide.Height);
        }

        /// <summary>Badges anchored at either horizontal edge clamp fully into the canvas.</summary>
        [Theory]
        [InlineData(2.0)]
        [InlineData(1598.0)]
        public void HorizontalEdgesClampIntoBounds(double anchorX)
        {
            BadgeRenderer.Layout layout = Measure([Small, Small], new Point(anchorX, 400), Wide);

            Assert.True(layout.Plate.X >= 0, "The plate must not extend past the left edge.");
            Assert.True(layout.Plate.Right <= Wide.Width, "The plate must not extend past the right edge.");
        }

        /// <summary>Non-readout badge plates can reuse the same edge clamping and top-edge flip.</summary>
        [Fact]
        public void FixedPlateUsesSharedViewportPlacement()
        {
            Rect plate = BadgeRenderer.Place(
                new Point(2, 4),
                Wide,
                width: 100,
                height: BadgeRenderer.Height);

            Assert.True(plate.X >= 0);
            Assert.True(plate.Y > 4);
            Assert.True(plate.Right <= Wide.Width);
        }

        /// <summary>No entries means no plate, so an inconsistent drag draws nothing.</summary>
        [Fact]
        public void NoEntriesMeansAnEmptyPlate()
        {
            BadgeRenderer.Layout layout = Measure([], new Point(800, 400), Wide);

            Assert.Empty(layout.Rows);
            Assert.Equal(0, layout.Plate.Width);
        }
    }
}
