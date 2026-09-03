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
    }
}
