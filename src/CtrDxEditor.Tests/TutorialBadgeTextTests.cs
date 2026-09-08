using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// TutorialBadgeText composes TutorialBadge.KeyFor's chosen key into the badge's full display string.
    /// This pure logic needs Localizer (Shared-only), so it lives here rather than in
    /// CtrDxEditor.Core.Tests; it does not touch DrawingContext, so it runs headlessly like any other
    /// unit test - no rendering target required.
    /// </summary>
    public class TutorialBadgeTextTests
    {
        private static LevelObject Prompt(params (string Name, string Value)[] attributes)
        {
            XElement element = new("tutorial01");
            foreach ((string attribute, string value) in attributes)
            {
                element.SetAttributeValue(attribute, value);
            }

            return new LevelObject(element);
        }

        /// <summary>A start prompt with nothing else authored needs no badge text at all.</summary>
        [Fact]
        public void StartPromptWithNothingElseHasNoText()
        {
            Assert.Null(TutorialBadgeText.For(Prompt()));
        }

        /// <summary>An edge event reads as a short "on X" phrase.</summary>
        [Fact]
        public void EdgeEventReadsAsOnPhrase()
        {
            Assert.Equal("on rope cut", TutorialBadgeText.For(Prompt(("showOn", "ropeCut"))));
        }

        /// <summary>A state event reads as a short "while X" phrase - never "on X", which would read wrong.</summary>
        [Fact]
        public void StateEventReadsAsWhilePhrase()
        {
            Assert.Equal("while bubbled", TutorialBadgeText.For(Prompt(("showOn", "bubbled"))));
        }

        /// <summary>Delay alone, with no trigger event, still reads as a complete clause.</summary>
        [Fact]
        public void DelayAloneReadsAsDelayedClause()
        {
            Assert.Equal("delayed 2s", TutorialBadgeText.For(Prompt(("delay", "2"))));
        }

        /// <summary>A sequencing group alone, with no trigger event, still reads as a complete clause.</summary>
        [Fact]
        public void GroupAloneReadsAsGroupClause()
        {
            Assert.Equal("group \"intro\"", TutorialBadgeText.For(Prompt(("group", "intro"))));
        }

        /// <summary>
        /// An unparseable showOn reads as its own distinct warning, never as a trigger name and never as
        /// silence that would be mistaken for a start prompt - the game drops this prompt entirely.
        /// </summary>
        [Fact]
        public void UnparseableShowOnReadsAsItsOwnWarning()
        {
            Assert.Equal("unknown trigger, never shows", TutorialBadgeText.For(Prompt(("showOn", "notARealEvent"))));
        }

        /// <summary>
        /// An unparseable showOn suppresses delay and group entirely: the whole prompt is dropped, so
        /// nothing else about it is worth composing into the same clause.
        /// </summary>
        [Fact]
        public void UnparseableShowOnSuppressesDelayAndGroup()
        {
            LevelObject prompt = Prompt(("showOn", "notARealEvent"), ("delay", "2"), ("group", "intro"));
            Assert.Equal("unknown trigger, never shows", TutorialBadgeText.For(prompt));
        }

        /// <summary>
        /// An event plus a delay plus a group compose into one clean, comma-joined line - no doubled
        /// preposition, no dangling separator - in event, delay, group order.
        /// </summary>
        [Fact]
        public void EventDelayAndGroupComposeInOrder()
        {
            LevelObject prompt = Prompt(("showOn", "lanternCatch"), ("delay", "2"), ("group", "a"));
            Assert.Equal("on lantern catch, delayed 2s, group \"a\"", TutorialBadgeText.For(prompt));
        }
    }
}
