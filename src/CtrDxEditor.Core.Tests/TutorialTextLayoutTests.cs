using System.Collections.Generic;

using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Greedy word-wrap used by the tutorial-text renderer with an injected measure function.</summary>
    public class TutorialTextLayoutTests
    {
        private static double Measure(string s)
        {
            return s.Length;
        }

        /// <summary>Keeps short text on one line.</summary>
        [Fact]
        public void ShortTextIsOneLine()
        {
            IReadOnlyList<string> lines = TutorialTextLayout.Wrap("hi there", 100, Measure);
            Assert.Equal(["hi there"], lines);
        }

        /// <summary>Wraps overflowing text on a word boundary.</summary>
        [Fact]
        public void WrapsOnWordBoundary()
        {
            IReadOnlyList<string> lines = TutorialTextLayout.Wrap("aaa bbb ccc", 7, Measure);
            Assert.Equal(["aaa bbb", "ccc"], lines);
        }

        /// <summary>Places a word wider than the limit on its own line.</summary>
        [Fact]
        public void LongWordGetsOwnLine()
        {
            IReadOnlyList<string> lines = TutorialTextLayout.Wrap("hi supercalifragilistic ok", 6, Measure);
            Assert.Equal(["hi", "supercalifragilistic", "ok"], lines);
        }

        /// <summary>Honors explicit newline characters.</summary>
        [Fact]
        public void ExplicitNewlineBreaks()
        {
            IReadOnlyList<string> lines = TutorialTextLayout.Wrap("a\nb", 100, Measure);
            Assert.Equal(["a", "b"], lines);
        }

        /// <summary>Returns no lines for empty text.</summary>
        [Fact]
        public void EmptyTextIsEmpty()
        {
            Assert.Empty(TutorialTextLayout.Wrap("", 100, Measure));
        }
    }
}
