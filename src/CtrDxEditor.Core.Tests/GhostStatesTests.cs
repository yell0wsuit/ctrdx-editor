using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests parsing of a ghost's enabled morph states.</summary>
    public class GhostStatesTests
    {
        private static LevelObject Ghost(string grab, string bubble, string bouncer)
        {
            LevelObject g = new(new XElement("ghost"));
            g.SetAttr("grab", grab);
            g.SetAttr("bubble", bubble);
            g.SetAttr("bouncer", bouncer);
            return g;
        }

        /// <summary>Enabled morphs come back in game cycle order: bubble, grab, bouncer.</summary>
        [Fact]
        public void EnabledReturnsMorphsInCycleOrder()
        {
            LevelObject g = Ghost("true", "true", "true");
            Assert.Equal([GhostMorph.Bubble, GhostMorph.Grab, GhostMorph.Bouncer], GhostStates.Enabled(g));
        }

        /// <summary>Disabled and missing bools are excluded.</summary>
        [Fact]
        public void EnabledExcludesDisabledStates()
        {
            LevelObject g = Ghost("true", "false", "false");
            Assert.Equal([GhostMorph.Grab], GhostStates.Enabled(g));
        }

        /// <summary>A ghost with no enabled states is idle-only.</summary>
        [Fact]
        public void IsIdleOnlyWhenNoStatesEnabled()
        {
            LevelObject g = Ghost("false", "false", "false");
            Assert.True(GhostStates.IsIdleOnly(g));
            Assert.Empty(GhostStates.Enabled(g));
        }

        /// <summary>Any enabled state means not idle-only.</summary>
        [Fact]
        public void IsNotIdleOnlyWithAState()
        {
            Assert.False(GhostStates.IsIdleOnly(Ghost("false", "true", "false")));
        }
    }
}
