using System.Linq;

using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tutorial trigger vocabulary, matching the game's TutorialEvents and TutorialSubjects.</summary>
    public class TutorialTriggerTests
    {
        /// <summary>Every XML spelling round-trips through the enum unchanged.</summary>
        [Theory]
        [InlineData("start", TutorialEvent.Start)]
        [InlineData("ropeCut", TutorialEvent.RopeCut)]
        [InlineData("bubbled", TutorialEvent.Bubbled)]
        [InlineData("candyMoved", TutorialEvent.CandyMoved)]
        [InlineData("gravityInverted", TutorialEvent.GravityInverted)]
        public void EventNamesRoundTrip(string xml, TutorialEvent expected)
        {
            Assert.True(TutorialEvents.TryParse(xml, out TutorialEvent parsed));
            Assert.Equal(expected, parsed);
            Assert.Equal(xml, TutorialEvents.Name(parsed));
        }

        /// <summary>Parsing is exact and case-sensitive, like the game's switch.</summary>
        [Theory]
        [InlineData("RopeCut")]
        [InlineData("ropecut")]
        [InlineData("notAnEvent")]
        [InlineData("")]
        public void UnknownEventNamesFail(string xml)
        {
            Assert.False(TutorialEvents.TryParse(xml, out _));
        }

        /// <summary>A null showOn defaults to start, as the loader does.</summary>
        [Fact]
        public void NullEventParsesAsStart()
        {
            Assert.True(TutorialEvents.TryParse(null, out TutorialEvent parsed));
            Assert.Equal(TutorialEvent.Start, parsed);
        }

        /// <summary>Bubbled and everything after it are continuously observable states.</summary>
        [Theory]
        [InlineData(TutorialEvent.Start, TutorialEventKind.Edge)]
        [InlineData(TutorialEvent.RopeCut, TutorialEventKind.Edge)]
        [InlineData(TutorialEvent.GravityFlip, TutorialEventKind.Edge)]
        [InlineData(TutorialEvent.Bubbled, TutorialEventKind.State)]
        [InlineData(TutorialEvent.CandyMoved, TutorialEventKind.State)]
        public void StateEventsStartAtBubbled(TutorialEvent value, TutorialEventKind expected)
        {
            Assert.Equal(expected, TutorialEvents.Kind(value));
        }

        /// <summary>All lists every event once, in the game's declaration order, for the dropdown.</summary>
        [Fact]
        public void AllListsEveryEventInOrder()
        {
            Assert.Equal(31, TutorialEvents.All.Count);
            Assert.Equal(TutorialEvent.Start, TutorialEvents.All[0]);
            Assert.Equal(TutorialEvent.CandyMoved, TutorialEvents.All[^1]);
            Assert.Equal(TutorialEvents.All.Count, TutorialEvents.All.Distinct().Count());
        }

        /// <summary>Every supported subject spelling parses to its matching semantic value.</summary>
        [Theory]
        [InlineData(null, TutorialSubject.Any)]
        [InlineData("any", TutorialSubject.Any)]
        [InlineData("primary", TutorialSubject.Primary)]
        [InlineData("left", TutorialSubject.Left)]
        [InlineData("right", TutorialSubject.Right)]
        public void SubjectNamesParse(string? xml, TutorialSubject expected)
        {
            Assert.True(TutorialSubjects.TryParse(xml, out TutorialSubject parsed));
            Assert.Equal(expected, parsed);
        }

        /// <summary>An unsupported subject spelling is rejected.</summary>
        [Fact]
        public void UnknownSubjectFails()
        {
            Assert.False(TutorialSubjects.TryParse("both", out _));
        }

        /// <summary>An area is four finite components in map coordinates.</summary>
        [Fact]
        public void AreaParsesFourComponents()
        {
            Assert.True(TutorialArea.TryParse("133,0,186,133", out TutorialArea area));
            Assert.Equal(133, area.X);
            Assert.Equal(0, area.Y);
            Assert.Equal(186, area.Width);
            Assert.Equal(133, area.Height);
            Assert.Equal("133,0,186,133", area.Format());
        }

        /// <summary>Both dimensions must be positive, and there must be exactly four components.</summary>
        [Theory]
        [InlineData("133,0,0,133")]
        [InlineData("133,0,186,-1")]
        [InlineData("133,0,186")]
        [InlineData("133,0,186,133,7")]
        [InlineData("a,0,186,133")]
        [InlineData("")]
        [InlineData(null)]
        public void InvalidAreasFail(string? value)
        {
            Assert.False(TutorialArea.TryParse(value, out _));
        }
    }
}
