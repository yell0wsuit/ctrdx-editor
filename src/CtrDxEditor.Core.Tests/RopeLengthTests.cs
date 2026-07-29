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
    }
}
