using System.Collections.Generic;

using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Wrapping follows the injected measure, which is how size reaches the line breaks.</summary>
    public class TutorialTextLayoutScaleTests
    {
        private const string Text = "Tap the screen to cut the rope and feed the candy to Om Nom";

        /// <summary>
        /// Wrap measures through a delegate, so a larger authored size has to reach it as a scaled
        /// measure rather than being applied to the glyphs after the fact.
        /// </summary>
        [Fact]
        public void LargerMeasureWrapsToMoreLines()
        {
            IReadOnlyList<string> atOne = TutorialTextLayout.Wrap(Text, 100, word => word.Length * 10);
            IReadOnlyList<string> atOnePointFour = TutorialTextLayout.Wrap(Text, 100, word => word.Length * 14);

            Assert.True(atOnePointFour.Count > atOne.Count);
        }

        /// <summary>Every word survives the wrap at either scale; only the breaks move.</summary>
        [Fact]
        public void WrappingPreservesEveryWord()
        {
            IReadOnlyList<string> lines = TutorialTextLayout.Wrap(Text, 100, word => word.Length * 14);

            Assert.Equal(Text.Split(' ').Length, string.Join(' ', lines).Split(' ').Length);
        }
    }
}
