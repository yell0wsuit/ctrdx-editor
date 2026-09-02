using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tutorial delay and fade envelope, matching TutorialPromptLoader.BuildEnvelope.</summary>
    public class TutorialTimingTests
    {
        private static LevelObject Prompt(string name, params (string Name, string Value)[] attributes)
        {
            XElement element = new(name);
            foreach ((string attribute, string value) in attributes)
            {
                element.SetAttributeValue(attribute, value);
            }

            return new LevelObject(element);
        }

        /// <summary>A sign holds 5.2s by default and text holds 5.0s, as the loader's defaults differ.</summary>
        [Fact]
        public void DefaultHoldDiffersBetweenTextAndSign()
        {
            Assert.Equal(5.0, TutorialTiming.For(Prompt("tutorialText")).Hold);
            Assert.Equal(5.2, TutorialTiming.For(Prompt("tutorial01")).Hold);
            Assert.Equal(1.0, TutorialTiming.For(Prompt("tutorial01")).FadeIn);
            Assert.Equal(0.5, TutorialTiming.For(Prompt("tutorial01")).FadeOut);
            Assert.Equal(0.0, TutorialTiming.For(Prompt("tutorial01")).Delay);
            Assert.Equal(1, TutorialTiming.For(Prompt("tutorial01")).Repeat);
        }

        /// <summary>Alpha ramps over fadeIn, holds at peak, then falls over fadeOut.</summary>
        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(0.5, 0.5)]
        [InlineData(1.0, 1.0)]
        [InlineData(3.0, 1.0)]
        [InlineData(6.0, 1.0)]
        [InlineData(6.25, 0.5)]
        [InlineData(6.5, 0.0)]
        [InlineData(9.0, 0.0)]
        public void EnvelopeRampsHoldsAndFalls(double seconds, double expected)
        {
            TutorialTiming timing = TutorialTiming.For(
                Prompt("tutorialText", ("fadeIn", "1"), ("duration", "5"), ("fadeOut", "0.5")));

            Assert.Equal(expected, timing.AlphaAt(seconds), 3);
        }

        /// <summary>Delay is consumed before the envelope starts, not inside it.</summary>
        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(1.9, 0.0)]
        [InlineData(2.0, 0.0)]
        [InlineData(2.5, 0.5)]
        [InlineData(3.0, 1.0)]
        public void DelayShiftsTheWholeEnvelope(double seconds, double expected)
        {
            TutorialTiming timing = TutorialTiming.For(
                Prompt("tutorialText", ("delay", "2"), ("fadeIn", "1"), ("duration", "5"), ("fadeOut", "0.5")));

            Assert.Equal(expected, timing.AlphaAt(seconds), 3);
        }

        /// <summary>duration="-1" fades in and stays up; there is no fade-out.</summary>
        [Theory]
        [InlineData(0.5, 0.5)]
        [InlineData(1.0, 1.0)]
        [InlineData(600.0, 1.0)]
        public void ForeverHoldNeverFadesOut(double seconds, double expected)
        {
            TutorialTiming timing = TutorialTiming.For(
                Prompt("tutorialText", ("fadeIn", "1"), ("duration", "-1")));

            Assert.True(timing.HoldsForever);
            Assert.Null(timing.TotalSeconds);
            Assert.Equal(expected, timing.AlphaAt(seconds), 3);
        }

        /// <summary>A repeat count replays the whole pass that many times, then stays clear.</summary>
        [Theory]
        [InlineData(0.5, 0.5)]
        [InlineData(6.6, 0.1)]
        [InlineData(7.5, 1.0)]
        [InlineData(13.5, 0.0)]
        public void RepeatReplaysThePass(double seconds, double expected)
        {
            TutorialTiming timing = TutorialTiming.For(
                Prompt("tutorialText", ("fadeIn", "1"), ("duration", "5"), ("fadeOut", "0.5"), ("repeat", "2")));

            Assert.Equal(6.5, timing.PassSeconds, 3);
            Assert.Equal(13.0, timing.TotalSeconds!.Value, 3);
            Assert.Equal(expected, timing.AlphaAt(seconds), 3);
        }

        /// <summary>repeat="-1" loops the pass for as long as the preview scrubs.</summary>
        [Fact]
        public void ForeverRepeatLoops()
        {
            TutorialTiming timing = TutorialTiming.For(
                Prompt("tutorialText", ("fadeIn", "1"), ("duration", "5"), ("fadeOut", "0.5"), ("repeat", "-1")));

            Assert.True(timing.RepeatsForever);
            Assert.Null(timing.TotalSeconds);
            Assert.Equal(timing.AlphaAt(0.5), timing.AlphaAt(7.0), 3);
        }

        /// <summary>A zero fade snaps rather than dividing by zero.</summary>
        [Fact]
        public void ZeroFadesSnap()
        {
            TutorialTiming timing = TutorialTiming.For(
                Prompt("tutorialText", ("fadeIn", "0"), ("duration", "2"), ("fadeOut", "0")));

            Assert.Equal(1.0, timing.AlphaAt(0.0), 3);
            Assert.Equal(1.0, timing.AlphaAt(1.9), 3);
            Assert.Equal(0.0, timing.AlphaAt(2.0), 3);
        }

        /// <summary>A malformed value falls back to the default rather than throwing.</summary>
        [Fact]
        public void MalformedValuesFallBackToDefaults()
        {
            TutorialTiming timing = TutorialTiming.For(
                Prompt("tutorialText", ("fadeIn", "banana"), ("duration", "-7"), ("repeat", "0")));

            Assert.Equal(1.0, timing.FadeIn);
            Assert.Equal(5.0, timing.Hold);
            Assert.Equal(1, timing.Repeat);
        }
    }
}
