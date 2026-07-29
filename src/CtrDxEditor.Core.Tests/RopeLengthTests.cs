using System;
using System.Collections.Generic;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests canvas editing of a grab's rope rest length.</summary>
    public class RopeLengthTests
    {
        private static LevelObject Grab(int x, int y, string length, params (string Name, string Value)[] extra)
        {
            XElement el = new("grab", new XAttribute("x", x), new XAttribute("y", y), new XAttribute("length", length));
            foreach ((string name, string value) in extra)
            {
                el.SetAttributeValue(name, value);
            }
            return new LevelObject(el);
        }

        private static LevelObject Candy(int x, int y)
        {
            return new LevelObject(new XElement("candy", new XAttribute("x", x), new XAttribute("y", y)));
        }

        private static RopeLength.Geometry Resolve(LevelObject grab, params LevelObject[] others)
        {
            List<LevelObject> objects = [grab, .. others];
            return RopeLength.Of(grab, RopeResolver.Resolve(grab, objects, twoParts: false))!.Value;
        }

        /// <summary>A bound grab resolves to its hook, its candy, and the gap between them.</summary>
        [Fact]
        public void BoundGrabResolvesEndpointsAndChord()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "200"), Candy(300, 400));

            Assert.Equal(new Vec2(0, 0), g.Hook);
            Assert.Equal(new Vec2(300, 400), g.Target);
            Assert.Equal(500, g.Chord, 6);
            Assert.Equal(200, g.Length, 6);
        }

        /// <summary>A rope no longer than the gap is taut; a longer one is not.</summary>
        [Theory]
        [InlineData("80", true)]
        [InlineData("100", true)]
        [InlineData("160", false)]
        public void TautTracksLengthAgainstTheChord(string length, bool expected)
        {
            Assert.Equal(expected, Resolve(Grab(0, 0, length), Candy(100, 0)).Taut);
        }

        /// <summary>A slack rope's knob hangs below the chord; a taut rope's sits on its midpoint.</summary>
        [Fact]
        public void KnobFollowsTheDrawnSag()
        {
            Assert.True(Resolve(Grab(0, 0, "200"), Candy(100, 0)).Knob.Y > 0);

            RopeLength.Geometry taut = Resolve(Grab(0, 0, "80"), Candy(100, 0));
            Assert.Equal(50, taut.Knob.X, 3);
            Assert.Equal(0, taut.Knob.Y, 3);
        }

        /// <summary>Grabs the game gives no authored rope have nothing to edit.</summary>
        [Fact]
        public void GrabsWithoutAnAuthoredRopeResolveToNull()
        {
            List<LevelObject> withCandy = [Candy(100, 0)];

            LevelObject gun = Grab(0, 0, "200", ("gun", "true"));
            Assert.Null(RopeLength.Of(gun, RopeResolver.Resolve(gun, [gun, .. withCandy], twoParts: false)));

            LevelObject autoCatch = Grab(0, 0, "200", ("radius", "120"));
            Assert.Null(RopeLength.Of(autoCatch, RopeResolver.Resolve(autoCatch, [autoCatch, .. withCandy], twoParts: false)));

            LevelObject unbound = Grab(0, 0, "200");
            Assert.Null(RopeLength.Of(unbound, RopeResolver.Resolve(unbound, [unbound], twoParts: false)));
        }

        /// <summary>A missing or unparsable length attribute reads as zero rather than throwing.</summary>
        [Fact]
        public void MissingLengthReadsAsZero()
        {
            LevelObject noLength = new(new XElement(
                "grab", new XAttribute("x", 0), new XAttribute("y", 0)));

            Assert.Equal(0, RopeLength.ReadLength(noLength));
            Assert.Equal(0, RopeLength.ReadLength(Grab(0, 0, "not-a-number")));
        }

        /// <summary>The knob is its own target, and it wins over the cord it sits on.</summary>
        [Fact]
        public void KnobWinsOverTheCordBeneathIt()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "200"), Candy(100, 0));

            (RopeLength.Handle handle, _) = RopeLength.HitTest(g, g.Knob, knobTolerance: 9, cordTolerance: 6);

            Assert.Equal(RopeLength.Handle.Knob, handle);
        }

        /// <summary>The cord is grabbable away from the knob, and empty space is not.</summary>
        [Fact]
        public void CordIsGrabbableAndEmptySpaceIsNot()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "200"), Candy(100, 0));
            Vec2 onCord = RopeStripBuilder.CalcPathBezier(
                RopeStripBuilder.ControlPoints(g.Hook, g.Target, g.Length), 0.25);

            (RopeLength.Handle onCordHandle, _) = RopeLength.HitTest(g, onCord, knobTolerance: 9, cordTolerance: 6);
            (RopeLength.Handle awayHandle, _) = RopeLength.HitTest(
                g, new Vec2(50, -400), knobTolerance: 9, cordTolerance: 6);

            Assert.Equal(RopeLength.Handle.Cord, onCordHandle);
            Assert.Equal(RopeLength.Handle.None, awayHandle);
        }

        /// <summary>The parameter is the point's projection along the chord, clamped away from the ends.</summary>
        [Fact]
        public void ParameterProjectsOntoTheChordAndClamps()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "200"), Candy(100, 0));

            Assert.Equal(0.4, RopeLength.Parameter(g, new Vec2(40, 999)), 6);
            Assert.Equal(RopeLength.MinParameter, RopeLength.Parameter(g, new Vec2(-50, 0)), 6);
            Assert.Equal(RopeLength.MaxParameter, RopeLength.Parameter(g, new Vec2(150, 0)), 6);
        }

        /// <summary>A hook sitting on its target has no usable chord, so the parameter degenerates safely.</summary>
        [Fact]
        public void DegenerateChordParameterIsTheMidpoint()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "200"), Candy(0, 0));

            Assert.Equal(0.5, RopeLength.Parameter(g, new Vec2(0, 60)), 6);
        }
    }
}
