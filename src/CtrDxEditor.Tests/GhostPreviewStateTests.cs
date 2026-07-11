using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the ephemeral ghost morph-preview controller.</summary>
    public class GhostPreviewStateTests
    {
        private static LevelObject Ghost(string grab, string bubble, string bouncer, string radius = "-1")
        {
            LevelObject g = new(new XElement("ghost"));
            g.SetAttr("grab", grab);
            g.SetAttr("bubble", bubble);
            g.SetAttr("bouncer", bouncer);
            g.SetAttr("radius", radius);
            return g;
        }

        /// <summary>Setting an enabled morph makes it active with the right sprite key.</summary>
        [Fact]
        public void SetEnabledMorphActivatesIt()
        {
            GhostPreviewState s = new();
            LevelObject g = Ghost("true", "false", "false", "100");
            s.Set(g, GhostMorph.Grab);

            Assert.Equal(GhostMorph.Grab, s.Active);
            Assert.Equal("grab", s.MorphSpriteKey);
            Assert.True(s.ShowsRadiusRing(g));
            Assert.Null(s.MorphHitboxElement); // a grab is a rope hook, no hitbox
        }

        /// <summary>Setting a disabled morph is ignored.</summary>
        [Fact]
        public void SetDisabledMorphIsIgnored()
        {
            GhostPreviewState s = new();
            LevelObject g = Ghost("true", "false", "false");
            s.Set(g, GhostMorph.Bouncer);

            Assert.Null(s.Active);
        }

        /// <summary>Radius ring is hidden when radius is the -1 auto sentinel even while previewing grab.</summary>
        [Fact]
        public void RadiusRingHiddenWhenAutoRope()
        {
            GhostPreviewState s = new();
            LevelObject g = Ghost("true", "false", "false", "-1");
            s.Set(g, GhostMorph.Grab);

            Assert.False(s.ShowsRadiusRing(g));
        }

        /// <summary>Bouncer preview shows the small bouncer sprite and the rotation dial.</summary>
        [Fact]
        public void BouncerPreviewShowsDialAndSmallSprite()
        {
            GhostPreviewState s = new();
            LevelObject g = Ghost("false", "false", "true");
            s.Set(g, GhostMorph.Bouncer);

            Assert.Equal("bouncer1", s.MorphSpriteKey);
            Assert.True(s.ShowsRotationDial(g));
            Assert.Equal("bouncer1", s.MorphHitboxElement);
        }

        /// <summary>Clear reverts to the plain ghost sprite.</summary>
        [Fact]
        public void ClearRevertsToGhost()
        {
            GhostPreviewState s = new();
            LevelObject g = Ghost("true", "false", "false", "100");
            s.Set(g, GhostMorph.Grab);
            s.Clear();

            Assert.Null(s.Active);
            Assert.Null(s.MorphSpriteKey);
        }
    }
}
