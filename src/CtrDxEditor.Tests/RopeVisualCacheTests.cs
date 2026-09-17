using System;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests that grab ropes are reused across frames only while nothing they are built from changes.</summary>
    public class RopeVisualCacheTests
    {
        private static readonly Type CacheType =
            typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.RopeVisualCache")!;

        /// <summary>An unchanged grab and target hand back the very rope built on the previous frame.</summary>
        [Fact]
        public void UnchangedRopeIsReused()
        {
            object cache = Activator.CreateInstance(CacheType, nonPublic: true)!;
            (LevelObject grab, LevelObject candy) = Pair();

            RopeVisual? first = Get(cache, grab, candy);
            RopeVisual? second = Get(cache, grab, candy);

            Assert.NotNull(first);
            Assert.Same(first, second);
        }

        /// <summary>Each input the rope is built from invalidates the cached rope when it changes.</summary>
        [Theory]
        [InlineData("grabX")]
        [InlineData("grabY")]
        [InlineData("length")]
        [InlineData("targetX")]
        [InlineData("chain")]
        [InlineData("skin")]
        [InlineData("physics")]
        public void ChangedInputRebuildsRope(string input)
        {
            object cache = Activator.CreateInstance(CacheType, nonPublic: true)!;
            (LevelObject grab, LevelObject candy) = Pair();
            RopeVisual? before = Get(cache, grab, candy);

            int skin = 0;
            RopePhysics physics = RopePhysics.Desktop;
            switch (input)
            {
                case "grabX":
                    grab.X += 10;
                    break;
                case "grabY":
                    grab.Y += 10;
                    break;
                case "length":
                    grab.SetAttr("length", "200");
                    break;
                case "targetX":
                    candy.X += 10;
                    break;
                case "chain":
                    grab.SetAttr(ChainRope.Attribute, "false");
                    break;
                case "skin":
                    skin = 1;
                    break;
                default:
                    physics = RopePhysics.Mobile;
                    break;
            }

            RopeVisual? after = Get(cache, grab, candy, physics, skin);

            Assert.NotNull(after);
            Assert.NotSame(before, after);
            // RopeVisual's record equality compares its lists by reference, so compare what they hold.
            RopeVisual expected = RopeRenderer(grab, candy, physics, skin)!;
            Assert.Equal(expected.SamplePoints, after.SamplePoints);
            Assert.Equal(expected.ChainSprites, after.ChainSprites);
            Assert.Equal(expected.Strips.Count, after.Strips.Count);
        }

        /// <summary>A grab with nothing to hang from has no rope, cached or not.</summary>
        [Fact]
        public void UnboundGrabHasNoRope()
        {
            object cache = Activator.CreateInstance(CacheType, nonPublic: true)!;
            LevelObject grab = new(XElement.Parse("""<grab x="100" y="100" length="90" radius="-1" />"""));

            RopeVisual? rope = (RopeVisual?)CacheType.GetMethod("Get")!.Invoke(
                cache, [grab, new RopeTarget(RopeTargetKind.None, null), RopePhysics.Desktop, 0]);

            Assert.Null(rope);
        }

        private static (LevelObject Grab, LevelObject Candy) Pair()
        {
            return (
                new LevelObject(XElement.Parse("""<grab x="100" y="100" length="90" radius="-1" />""")),
                new LevelObject(XElement.Parse("""<candy x="120" y="250" />""")));
        }

        private static RopeVisual? Get(
            object cache, LevelObject grab, LevelObject candy, RopePhysics? physics = null, int skin = 0)
        {
            return (RopeVisual?)CacheType.GetMethod("Get")!.Invoke(
                cache, [grab, new RopeTarget(RopeTargetKind.Candy, candy), physics ?? RopePhysics.Desktop, skin]);
        }

        // A fresh build to compare the cache's answer against, so a rebuilt rope is also the right rope.
        private static RopeVisual? RopeRenderer(LevelObject grab, LevelObject candy, RopePhysics physics, int skin)
        {
            Type renderer = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.RopeRenderer")!;
            MethodInfo build = renderer.GetMethod(
                "BuildRope", [typeof(LevelObject), typeof(RopeTarget), typeof(RopePhysics), typeof(int)])!;
            return (RopeVisual?)build.Invoke(null, [grab, new RopeTarget(RopeTargetKind.Candy, candy), physics, skin]);
        }
    }
}
