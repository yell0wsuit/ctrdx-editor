using CtrDxEditor.UsageGuide;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the constrained emphasis syntax used by Usage Guide body copy.</summary>
    public class GuideTextParserTests
    {
        /// <summary>Unmarked text remains one ordinary run.</summary>
        [Fact]
        public void PlainTextRemainsUnstyled()
        {
            Assert.Equal(
                [new GuideTextRun("Move the candy.")],
                GuideTextParser.Parse("Move the candy."));
        }

        /// <summary>Double asterisks produce a bold run.</summary>
        [Fact]
        public void DoubleAsterisksMakeTextBold()
        {
            Assert.Equal(
                [new GuideTextRun("Drag ", false, false), new GuideTextRun("carefully", true, false)],
                GuideTextParser.Parse("Drag **carefully**"));
        }

        /// <summary>Single asterisks produce an italic run.</summary>
        [Fact]
        public void SingleAsterisksMakeTextItalic()
        {
            Assert.Equal(
                [new GuideTextRun("Use ", false, false), new GuideTextRun("Preview", false, true)],
                GuideTextParser.Parse("Use *Preview*"));
        }

        /// <summary>Triple asterisks combine bold and italic.</summary>
        [Fact]
        public void TripleAsterisksMakeTextBoldItalic()
        {
            Assert.Equal(
                [new GuideTextRun("Never ", false, false), new GuideTextRun("overwrite", true, true)],
                GuideTextParser.Parse("Never ***overwrite***"));
        }

        /// <summary>Different emphasis forms retain their source order.</summary>
        [Fact]
        public void MixedFormattingProducesOrderedRuns()
        {
            GuideTextRun[] expected =
            [
                new("Choose "),
                new("File", true),
                new(", then "),
                new("Save As", IsItalic: true),
                new("."),
            ];

            Assert.Equal(
                expected,
                GuideTextParser.Parse("Choose **File**, then *Save As*."));
        }

        /// <summary>An opening marker without a matching close remains readable.</summary>
        [Fact]
        public void UnmatchedMarkerRemainsLiteralText()
        {
            Assert.Equal(
                [new GuideTextRun("Keep *this marker")],
                GuideTextParser.Parse("Keep *this marker"));
        }

        /// <summary>A malformed bold opener cannot be reinterpreted as an italic opener.</summary>
        [Fact]
        public void UnmatchedMultiCharacterMarkerRemainsLiteralText()
        {
            Assert.Equal(
                [new GuideTextRun("Keep **this *marker*")],
                GuideTextParser.Parse("Keep **this *marker*"));
        }
    }
}
