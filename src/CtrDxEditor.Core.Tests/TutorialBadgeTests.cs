using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Preview fires every prompt at zero, so the badge carries the real trigger.</summary>
    public class TutorialBadgeTests
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

        /// <summary>A prompt that plays from the start needs no annotation.</summary>
        [Fact]
        public void StartPromptsHaveNoBadge()
        {
            Assert.Null(TutorialBadge.KeyFor(Prompt()));
            Assert.Null(TutorialBadge.KeyFor(Prompt(("showOn", "start"))));
        }

        /// <summary>Edge and state events read differently, so they use different phrasings.</summary>
        [Fact]
        public void EdgeAndStateEventsUseDifferentKeys()
        {
            Assert.Equal("Canvas.Tutorial.Badge.Edge", TutorialBadge.KeyFor(Prompt(("showOn", "ropeCut"))));
            Assert.Equal("Canvas.Tutorial.Badge.State", TutorialBadge.KeyFor(Prompt(("showOn", "bubbled"))));
        }

        /// <summary>A delay alone is worth annotating even with no trigger.</summary>
        [Fact]
        public void DelayAloneEarnsABadge()
        {
            Assert.NotNull(TutorialBadge.KeyFor(Prompt(("delay", "2"))));
        }

        /// <summary>A sequencing group alone is worth annotating even with no trigger or delay.</summary>
        [Fact]
        public void GroupAloneEarnsABadge()
        {
            Assert.Equal("Canvas.Tutorial.Badge.Group", TutorialBadge.KeyFor(Prompt(("group", "intro"))));
        }

        /// <summary>
        /// An unparseable showOn is not "start": the game's loader (skipInvalid: true) drops the whole
        /// prompt when TutorialEvent.Parse throws, so it never plays at all. A badge that fell back to no
        /// annotation here would read as "plays at start" - the exact misrepresentation this badge exists
        /// to prevent - so it gets its own distinct key instead.
        /// </summary>
        [Fact]
        public void UnparseableShowOnGetsItsOwnKey()
        {
            Assert.Equal("Canvas.Tutorial.Badge.Invalid", TutorialBadge.KeyFor(Prompt(("showOn", "notARealEvent"))));
        }

        /// <summary>
        /// An unparseable showOn wins over every other reason to show a badge: the whole prompt is
        /// dropped, so an authored delay or group is moot and must not be shown instead.
        /// </summary>
        [Fact]
        public void UnparseableShowOnTakesPriorityOverDelayAndGroup()
        {
            LevelObject prompt = Prompt(("showOn", "notARealEvent"), ("delay", "2"), ("group", "intro"));
            Assert.Equal("Canvas.Tutorial.Badge.Invalid", TutorialBadge.KeyFor(prompt));
        }
    }
}
