using System.Collections.Generic;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tutorial motion mode, leg timing and easing, matching the game's TutorialMotion.</summary>
    public class TutorialMotionTests
    {
        private static LevelObject Prompt(params (string Name, string Value)[] attributes)
        {
            XElement element = new("tutorial10");
            foreach ((string attribute, string value) in attributes)
            {
                element.SetAttributeValue(attribute, value);
            }

            return new LevelObject(element);
        }

        /// <summary>No path is no motion, even when speeds are authored - CTRMover.FromXml returns null.</summary>
        [Fact]
        public void NoPathIsNoMotion()
        {
            Assert.Equal(
                TutorialMotionMode.None,
                TutorialMotion.ModeOf(Prompt(("moveSpeed", "100"), ("rotateSpeed", "100"))));
            Assert.Equal(TutorialMotionMode.None, TutorialMotion.ModeOf(Prompt(("path", ""))));
        }

        /// <summary>A bare path runs the shared mover, which loops independently of the fade.</summary>
        [Fact]
        public void BarePathIsLooping()
        {
            Assert.Equal(
                TutorialMotionMode.Looping,
                TutorialMotion.ModeOf(Prompt(("path", "230,0"), ("moveSpeed", "100"))));
        }

        /// <summary>Any of ease, moveDelay or repeat alongside a path switches to timeline motion.</summary>
        [Theory]
        [InlineData("ease", "in")]
        [InlineData("moveDelay", "1.5")]
        [InlineData("repeat", "2")]
        public void TimingAttributesSwitchToTimedMotion(string attribute, string value)
        {
            Assert.Equal(
                TutorialMotionMode.Timed,
                TutorialMotion.ModeOf(Prompt(("path", "230,0"), (attribute, value))));
        }

        /// <summary>Leg duration is distance over speed, and travel includes the leading delay.</summary>
        [Fact]
        public void LegSecondsAreDistanceOverSpeed()
        {
            TutorialMotion motion = TutorialMotion.Timed(
                Prompt(("path", "230,0,440,0"), ("moveSpeed", "440"), ("ease", "in,out"), ("moveDelay", "1.5")))!;

            Assert.Equal(2, motion.LegSeconds.Count);
            Assert.Equal(230.0 / 440.0, motion.LegSeconds[0], 4);
            Assert.Equal(210.0 / 440.0, motion.LegSeconds[1], 4);
            Assert.Equal(1.5 + 1.0, motion.TravelSeconds, 4);
            Assert.Equal([TutorialEase.In, TutorialEase.Out], motion.Eases);
        }

        /// <summary>A single ease value applies to every leg, the shorthand migrated content uses.</summary>
        [Fact]
        public void SingleEaseAppliesToEveryLeg()
        {
            TutorialMotion motion = TutorialMotion.Timed(
                Prompt(("path", "100,0,200,0,300,0"), ("ease", "in")))!;

            Assert.Equal([TutorialEase.In, TutorialEase.In, TutorialEase.In], motion.Eases);
        }

        /// <summary>
        /// Each leg's marker sits at the anchor-relative midpoint of that leg alone, matched to that leg's
        /// own ease - not the previous leg's, and not the path's overall midpoint.
        /// </summary>
        [Fact]
        public void LegMarkersPairEachLegsMidpointWithItsOwnEase()
        {
            TutorialMotion motion = TutorialMotion.Timed(
                Prompt(("path", "230,0,440,0"), ("moveSpeed", "440"), ("ease", "in,out")))!;

            IReadOnlyList<(Vec2 Midpoint, TutorialEase Ease)> markers = motion.LegMarkers;

            Assert.Equal(2, markers.Count);
            Assert.Equal(new Vec2(115, 0), markers[0].Midpoint); // (0,0) -> (230,0)
            Assert.Equal(TutorialEase.In, markers[0].Ease);
            Assert.Equal(new Vec2(335, 0), markers[1].Midpoint); // (230,0) -> (440,0)
            Assert.Equal(TutorialEase.Out, markers[1].Ease);
        }

        /// <summary>A three-leg path with a single shorthand ease marks every leg's own midpoint the same way.</summary>
        [Fact]
        public void LegMarkersCoverEveryLegOnAMultiLegPath()
        {
            TutorialMotion motion = TutorialMotion.Timed(
                Prompt(("path", "100,0,100,100,0,100"), ("ease", "none")))!;

            IReadOnlyList<(Vec2 Midpoint, TutorialEase Ease)> markers = motion.LegMarkers;

            Assert.Equal(3, markers.Count);
            Assert.Equal(new Vec2(50, 0), markers[0].Midpoint);
            Assert.Equal(new Vec2(100, 50), markers[1].Midpoint);
            Assert.Equal(new Vec2(50, 100), markers[2].Midpoint);
            Assert.All(markers, m => Assert.Equal(TutorialEase.None, m.Ease));
        }

        /// <summary>Easing integrates the game's constant acceleration: p squared, and its mirror.</summary>
        [Theory]
        [InlineData(TutorialEase.None, 0.25, 0.25)]
        [InlineData(TutorialEase.In, 0.5, 0.25)]
        [InlineData(TutorialEase.Out, 0.5, 0.75)]
        [InlineData(TutorialEase.In, 1.0, 1.0)]
        [InlineData(TutorialEase.Out, 1.0, 1.0)]
        public void EasingMatchesTheGamesCurves(TutorialEase ease, double progress, double expected)
        {
            Assert.Equal(expected, TutorialMotion.EaseProgress(ease, progress), 4);
        }

        /// <summary>Position holds at the anchor through moveDelay, travels, then rests on the last offset.</summary>
        [Fact]
        public void PositionHoldsThenTravelsThenRests()
        {
            Vec2 anchor = new(93, 149);
            TutorialMotion motion = TutorialMotion.Timed(
                Prompt(("path", "220,0"), ("moveSpeed", "440"), ("moveDelay", "1.0")))!;

            Assert.Equal(anchor, motion.PositionAt(0.0, anchor));
            Assert.Equal(anchor, motion.PositionAt(1.0, anchor));
            Assert.Equal(new Vec2(203, 149), motion.PositionAt(1.25, anchor));
            Assert.Equal(new Vec2(313, 149), motion.PositionAt(1.5, anchor));
            Assert.Equal(new Vec2(313, 149), motion.PositionAt(99.0, anchor));
        }

        /// <summary>An eased leg lands exactly on its target rather than stopping short.</summary>
        [Fact]
        public void EasedLegLandsOnItsTarget()
        {
            Vec2 anchor = new(0, 0);
            TutorialMotion motion = TutorialMotion.Timed(
                Prompt(("path", "100,0"), ("moveSpeed", "100"), ("ease", "in")))!;

            Assert.Equal(new Vec2(100, 0), motion.PositionAt(1.0, anchor));
        }

        /// <summary>A circular path is not expressible as timeline motion; the game fails to parse it.</summary>
        [Fact]
        public void CircularPathIsNotTimedMotion()
        {
            Assert.Null(TutorialMotion.Timed(Prompt(("path", "R50"), ("ease", "in"))));
        }

        /// <summary>An ease list that does not cover every leg is not usable motion.</summary>
        [Fact]
        public void MismatchedEaseCountIsNotUsable()
        {
            Assert.Null(TutorialMotion.Timed(Prompt(("path", "100,0,200,0"), ("ease", "in,out,none"))));
        }

        /// <summary>
        /// A genuinely circular path (RC.../RW...) is rejected by the IsCircularPath guard itself.
        /// "R50" in CircularPathIsNotTimedMotion never reaches this branch - RelativePolyline.Points
        /// can't parse a comma-free string into offsets, so that test passes via the separate
        /// zero-offsets guard instead. This test forces IsCircularPath's true branch specifically.
        /// </summary>
        [Fact]
        public void GenuinelyCircularPathIsRejectedByTheCircularGuard()
        {
            Assert.Null(TutorialMotion.Timed(Prompt(("path", "RC50"), ("ease", "in"))));
        }

        /// <summary>
        /// A mid-leg eased position lands at the eased fraction, not the linear one. The second leg
        /// only moves in Y (X target equals the previous point), so a swapped X/Y axis would show up
        /// as X drifting when it should hold steady. Using a different ease per leg (none, then in)
        /// also catches a wrong per-leg Eases index, since linear (0.5) and eased-in (0.25) diverge.
        /// </summary>
        [Fact]
        public void EasedMidLegUsesTheLegsOwnEaseNotLinear()
        {
            Vec2 anchor = new(10, 20);
            TutorialMotion motion = TutorialMotion.Timed(
                Prompt(("path", "100,0,100,100"), ("moveSpeed", "100"), ("ease", "none,in")))!;

            Assert.Equal(new Vec2(110, 45), motion.PositionAt(1.5, anchor));
        }
    }
}
