using System;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// <c>LevelSceneRenderer.DrawOffset</c>'s tutorial-specific branches: a Timed prompt runs its own
    /// eased timeline instead of the shared mover, replaying once per envelope pass, while a Looping
    /// prompt (a bare path with none of ease/moveDelay/repeat authored) rides the very same mover every
    /// other pathed object uses.
    /// </summary>
    public class TutorialTimedMotionPreviewTests
    {
        private static readonly Type SceneRenderer =
            typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;

        private static Vec2 DrawOffset(LevelObject obj, double? previewSeconds)
        {
            MethodInfo method = SceneRenderer.GetMethod("DrawOffset", BindingFlags.Public | BindingFlags.Static)!;
            return (Vec2)method.Invoke(null, [obj, previewSeconds])!;
        }

        private static LevelObject Prompt(string type, params (string Name, string Value)[] attributes)
        {
            XElement element = new(type);
            foreach ((string name, string value) in attributes)
            {
                element.SetAttributeValue(name, value);
            }

            return new LevelObject(element);
        }

        /// <summary>
        /// Halfway through the first leg, after moveDelay has elapsed: distance 220 at speed 440 is a
        /// 0.5s leg, delayed a further 1.0s, so at t=1.25 the leg is exactly half travelled (fraction
        /// 0.5) with no easing authored - offset.X = 0.5 * 220 = 110, worked out from the raw
        /// distance/speed/time arithmetic rather than by re-running PositionAt's own code.
        /// </summary>
        [Fact]
        public void TimedModeAdvancesAlongTheAuthoredLegAfterItsDelay()
        {
            LevelObject prompt = Prompt(
                "tutorial10",
                ("x", "0"), ("y", "0"),
                ("path", "220,0"), ("moveSpeed", "440"), ("moveDelay", "1.0"), ("ease", "none"));

            Vec2 offset = DrawOffset(prompt, 1.25);

            Assert.Equal(110.0, offset.X, 6);
            Assert.Equal(0.0, offset.Y, 6);
        }

        /// <summary>
        /// Before its delay elapses, a Timed prompt has not begun travelling - the offset is exactly
        /// zero, not merely close to it.
        /// </summary>
        [Fact]
        public void TimedModeStaysAtAnchorDuringItsDelay()
        {
            LevelObject prompt = Prompt(
                "tutorial10",
                ("x", "50"), ("y", "60"),
                ("path", "220,0"), ("moveSpeed", "440"), ("moveDelay", "1.0"), ("ease", "none"));

            Vec2 offset = DrawOffset(prompt, 0.5);

            Assert.Equal(default, offset);
        }

        /// <summary>
        /// A finite <c>repeat</c> replays the same travel every pass rather than only playing through
        /// once: with fadeIn=1, duration(hold)=1, fadeOut=1 the pass is exactly 3s, so t=0.5 (in pass
        /// one) and t=3.5 (0.5s into pass two) sit at the same point in their own pass and must report
        /// the identical offset. A naive implementation that fed raw elapsed seconds straight into
        /// PositionAt would instead report the resting position at the end of the (single) leg for
        /// t=3.5, since by then the un-repeated leg has long finished - so this also distinguishes the
        /// wraparound from that simpler, wrong implementation.
        /// </summary>
        [Fact]
        public void FiniteRepeatReplaysTheSameTravelEveryPass()
        {
            LevelObject prompt = Prompt(
                "tutorial10",
                ("x", "0"), ("y", "0"),
                ("path", "100,0"), ("moveSpeed", "100"), ("ease", "none"),
                ("fadeIn", "1"), ("duration", "1"), ("fadeOut", "1"), ("repeat", "2"));

            Vec2 firstPass = DrawOffset(prompt, 0.5);
            Vec2 secondPass = DrawOffset(prompt, 3.5);

            Assert.Equal(50.0, firstPass.X, 6);
            Assert.Equal(firstPass, secondPass);
        }

        /// <summary>
        /// Looping mode (a bare path with no ease/moveDelay/repeat) is explicitly the shared mover, not
        /// a tutorial-specific system: an ordinary pathed object with identical position/path/moveSpeed
        /// must report the exact same offset at the exact same elapsed time.
        /// </summary>
        [Fact]
        public void LoopingModeMatchesTheOrdinarySharedMover()
        {
            LevelObject tutorialMover = Prompt(
                "tutorial10", ("x", "40"), ("y", "80"), ("path", "230,0"), ("moveSpeed", "100"));
            LevelObject ordinaryMover = Prompt(
                "star", ("x", "40"), ("y", "80"), ("path", "230,0"), ("moveSpeed", "100"));

            Vec2 tutorialOffset = DrawOffset(tutorialMover, 1.75);
            Vec2 ordinaryOffset = DrawOffset(ordinaryMover, 1.75);

            Assert.Equal(ordinaryOffset, tutorialOffset);
            Assert.NotEqual(default, tutorialOffset);
        }

        /// <summary>A prompt with no authored path (Mode None) never moves, whatever the elapsed time.</summary>
        [Fact]
        public void NoPathTutorialNeverMoves()
        {
            LevelObject prompt = Prompt("tutorialText", ("x", "10"), ("y", "20"), ("text", "Hi"));

            Assert.Equal(default, DrawOffset(prompt, 5.0));
        }

        /// <summary>With preview off, a Timed prompt draws exactly where authored.</summary>
        [Fact]
        public void PreviewOffLeavesTimedPromptAtItsAnchor()
        {
            LevelObject prompt = Prompt(
                "tutorial10", ("x", "0"), ("y", "0"), ("path", "220,0"), ("ease", "none"));

            Assert.Equal(default, DrawOffset(prompt, null));
        }
    }
}
