using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Tests the chain rope: reading <c>breakable</c> the way the game's <c>LoadGrab</c> does, and
    /// laying out the links the way <c>Bungee.BuildChainSpritePlan</c> does.
    /// </summary>
    public class ChainRopeTests
    {
        private static LevelObject Obj(string xml)
        {
            return new(XElement.Parse(xml));
        }

        private static ObjectDescriptor GrabDescriptor()
        {
            ObjectDescriptor? descriptor = DescriptorTable.CtrObjects.For("grab");
            Assert.NotNull(descriptor);
            return descriptor;
        }

        /// <summary>The grab exposes breakable, defaulting to the game's own true.</summary>
        [Fact]
        public void GrabExposesBreakableDefaultingToTrue()
        {
            AttributeSpec breakable = Assert.Single(GrabDescriptor().Attributes, a => a.Name == "breakable");

            Assert.Equal(AttrType.Bool, breakable.Type);
            Assert.Equal("true", breakable.Default);
        }

        /// <summary>A hand-written chain round-trips unchanged; the editor rewrites nothing it did not author.</summary>
        [Fact]
        public void ChainAttributeSurvivesARoundTrip()
        {
            string xml = """
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="1024" height="576" />
                        <gameDesign />
                    </layer>
                    <layer name="Objects"><candy x="178" y="178" /><target x="300" y="400" /><grab x="181" y="87" length="55" breakable="false" /></layer>
                </map>
                """;

            LevelDocument doc = LevelDocument.Parse(xml);

            Assert.True(XNode.DeepEquals(XDocument.Parse(xml), XDocument.Parse(doc.Save())));
        }

        /// <summary>A grab with no breakable attribute is an ordinary rope; the game defaults it to true.</summary>
        [Fact]
        public void MissingBreakableIsNotAChain()
        {
            Assert.False(ChainRope.IsChain(Obj("""<grab x="1" y="1" length="50" />""")));
        }

        /// <summary>Only a falsy breakable marks a chain; the game accepts "1" for true as well.</summary>
        [Theory]
        [InlineData("false", true)]
        [InlineData("False", true)]
        [InlineData("0", true)]
        [InlineData("", true)]
        [InlineData("true", false)]
        [InlineData("True", false)]
        [InlineData("1", false)]
        public void BreakableIsReadAsTheGameReadsIt(string value, bool expected)
        {
            Assert.Equal(expected, ChainRope.IsChain(Obj($"""<grab x="1" y="1" breakable="{value}" />""")));
        }

        /// <summary>Only a grab can be a chain, whatever another element's attributes say.</summary>
        [Fact]
        public void NonGrabIsNeverAChain()
        {
            Assert.False(ChainRope.IsChain(Obj("""<candy x="1" y="1" breakable="false" />""")));
        }

        /// <summary>Turning the chain on writes the explicit false the game needs.</summary>
        [Fact]
        public void SettingChainWritesBreakableFalse()
        {
            LevelObject grab = Obj("""<grab x="1" y="1" />""");

            ChainRope.Set(grab, chain: true);

            Assert.Equal("false", grab.GetAttr("breakable"));
            Assert.True(ChainRope.IsChain(grab));
        }

        /// <summary>Turning it off removes the attribute rather than writing the game's own default.</summary>
        [Fact]
        public void ClearingChainRemovesTheAttribute()
        {
            LevelObject grab = Obj("""<grab x="1" y="1" breakable="false" />""");

            ChainRope.Set(grab, chain: false);

            Assert.Null(grab.GetAttr("breakable"));
            Assert.False(ChainRope.IsChain(grab));
        }

        /// <summary>
        /// The plan holds one link per bezier sample plus one between each adjacent pair, at the game's
        /// 2 samples per control-point segment.
        /// </summary>
        [Fact]
        public void PlanHasALinkPerSampleAndOnePerGap()
        {
            Vec2[] controls = [.. RopeStripBuilder.ControlPoints(new Vec2(0, 0), new Vec2(300, 0), 290)];
            int sampleCount = (controls.Length - 1) * 2;

            IReadOnlyList<ChainSprite> plan = ChainSpritePlanner.Build(controls, seed: 1);

            Assert.Equal(sampleCount + (sampleCount - 1), plan.Count);
            Assert.Equal(sampleCount, plan.Count(s => s.QuadIndex == ChainSpritePlanner.LinkQuad));
            Assert.Equal(sampleCount - 1, plan.Count(s => s.QuadIndex == ChainSpritePlanner.MidpointQuad));
        }

        /// <summary>
        /// The link count follows the rope's part count, so it has to match what the game subdivides.
        /// A default-length rope carries five parts, which is eight samples: eight links and the seven
        /// midpoints between them.
        /// </summary>
        [Theory]
        [InlineData(0, 3)]     // two parts: two links, one midpoint
        [InlineData(35, 7)]    // three parts
        [InlineData(100, 15)]  // the default authored rope length: five parts
        [InlineData(210, 27)]  // eight parts
        public void LinkCountFollowsTheGameSubdivision(double length, int expected)
        {
            IReadOnlyList<ChainSprite> plan =
                ChainSpritePlanner.Build(new Vec2(0, 0), new Vec2(300, 0), length, seed: 1);

            Assert.Equal(expected, plan.Count);
        }

        /// <summary>Links come first, then the midpoints, matching the game's fill order.</summary>
        [Fact]
        public void LinksArePlannedBeforeMidpoints()
        {
            IReadOnlyList<ChainSprite> plan = ChainSpritePlanner.Build(new Vec2(0, 0), new Vec2(300, 0), 290, seed: 1);
            int firstMidpoint = plan.ToList().FindIndex(s => s.QuadIndex == ChainSpritePlanner.MidpointQuad);

            Assert.All(plan.Take(firstMidpoint), s => Assert.Equal(ChainSpritePlanner.LinkQuad, s.QuadIndex));
            Assert.All(plan.Skip(firstMidpoint), s => Assert.Equal(ChainSpritePlanner.MidpointQuad, s.QuadIndex));
        }

        /// <summary>The first link starts at the grab, and the last stops one step short of the target.</summary>
        [Fact]
        public void LinksRunFromTheGrabTowardTheTarget()
        {
            Vec2 from = new(0, 0);
            Vec2 to = new(300, 0);
            IReadOnlyList<ChainSprite> plan = ChainSpritePlanner.Build(from, to, length: 300, seed: 1);
            List<ChainSprite> links = [.. plan.Where(s => s.QuadIndex == ChainSpritePlanner.LinkQuad)];

            Assert.Equal(from, links[0].Center);
            Assert.InRange(links[^1].Center.X, 0, to.X);
            Assert.True(links[^1].Center.X > links[0].Center.X);
        }

        /// <summary>The first link takes no rotation, exactly as the game leaves index 0 at angle 0.</summary>
        [Fact]
        public void FirstLinkIsUnrotated()
        {
            IReadOnlyList<ChainSprite> plan = ChainSpritePlanner.Build(new Vec2(0, 0), new Vec2(300, 0), 290, seed: 1);

            Assert.Equal(0, plan[0].Rotation);
        }

        /// <summary>
        /// A link's angle is the game's atan2(prev - current) + 90 degrees. On a taut horizontal rope
        /// running left to right that is a quarter turn.
        /// </summary>
        [Fact]
        public void LinkAngleMatchesTheGameFormula()
        {
            IReadOnlyList<ChainSprite> plan = ChainSpritePlanner.Build(new Vec2(0, 0), new Vec2(300, 0), length: 0, seed: 1);

            Assert.Equal(Math.Atan2(0, -1) + (Math.PI / 2), plan[1].Rotation, 6);
        }

        /// <summary>Every link is either left white or shaded a grey in the game's [0.5, 1] range.</summary>
        [Fact]
        public void LinkTintsAreWhiteOrGrey()
        {
            IReadOnlyList<ChainSprite> plan = ChainSpritePlanner.Build(new Vec2(0, 0), new Vec2(300, 120), 340, seed: 12345);

            Assert.All(plan, s =>
            {
                Assert.Equal(s.Tint.R, s.Tint.G);
                Assert.Equal(s.Tint.G, s.Tint.B);
                Assert.InRange(s.Tint.R, 0.5, 1.0);
                Assert.Equal(1, s.Tint.A);
            });
            Assert.Contains(plan, s => s.Tint.R == 1);
        }

        /// <summary>The same seed always produces the same tints, so links never flicker across redraws.</summary>
        [Fact]
        public void TintsAreStableForASeed()
        {
            IReadOnlyList<ChainSprite> first = ChainSpritePlanner.Build(new Vec2(0, 0), new Vec2(300, 120), 340, seed: 7);
            IReadOnlyList<ChainSprite> second = ChainSpritePlanner.Build(new Vec2(0, 0), new Vec2(300, 120), 340, seed: 7);

            Assert.Equal(first, second);
        }

        /// <summary>Different ropes shade differently, so a level of chains is not one repeated pattern.</summary>
        [Fact]
        public void DifferentSeedsShadeDifferently()
        {
            IReadOnlyList<ChainSprite> a = ChainSpritePlanner.Build(new Vec2(0, 0), new Vec2(300, 120), 340, seed: 7);
            IReadOnlyList<ChainSprite> b = ChainSpritePlanner.Build(new Vec2(0, 0), new Vec2(300, 120), 340, seed: 8);

            Assert.NotEqual(a.Select(s => s.Tint), b.Select(s => s.Tint));
        }

        /// <summary>A chain hangs on the same curve a cord of the same length would.</summary>
        [Fact]
        public void ChainSagsLikeTheCord()
        {
            Vec2 from = new(0, 0);
            Vec2 to = new(200, 0);
            IReadOnlyList<ChainSprite> slack = ChainSpritePlanner.Build(from, to, length: 400, seed: 1);
            IReadOnlyList<ChainSprite> taut = ChainSpritePlanner.Build(from, to, length: 10, seed: 1);

            Assert.True(slack.Max(s => s.Center.Y) > taut.Max(s => s.Center.Y));
        }

        /// <summary>Too few control points to draw between yields no links rather than throwing.</summary>
        [Fact]
        public void DegenerateCurveProducesNoLinks()
        {
            Assert.Empty(ChainSpritePlanner.Build([], seed: 1));
            Assert.Empty(ChainSpritePlanner.Build([new Vec2(0, 0)], seed: 1));
        }
    }
}
