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

        /// <summary>A cord hit reports the curve parameter it landed on, not a projection onto the chord.</summary>
        [Fact]
        public void CordHitReportsTheCurveParameter()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "400"), Candy(200, 150));
            Vec2[] controls = RopeStripBuilder.ControlPoints(g.Hook, g.Target, g.Length);

            (_, double t) = RopeLength.HitTest(
                g, RopeStripBuilder.CalcPathBezier(controls, 0.3), knobTolerance: 9, cordTolerance: 6);

            Assert.Equal(0.3, t, 2);
        }

        /// <summary>
        /// The cord's ends are refused rather than clamped into range. Clamping would anchor the drag to a
        /// parameter the cursor is not on, so the length would jump the moment the press landed.
        /// </summary>
        [Fact]
        public void CordEndsAreNotGrabbable()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "400"), Candy(200, 150));
            Vec2[] controls = RopeStripBuilder.ControlPoints(g.Hook, g.Target, g.Length);

            (RopeLength.Handle nearHook, _) = RopeLength.HitTest(
                g, RopeStripBuilder.CalcPathBezier(controls, 0.02), knobTolerance: 9, cordTolerance: 6);
            (RopeLength.Handle nearTarget, _) = RopeLength.HitTest(
                g, RopeStripBuilder.CalcPathBezier(controls, 0.98), knobTolerance: 9, cordTolerance: 6);

            Assert.Equal(RopeLength.Handle.None, nearHook);
            Assert.Equal(RopeLength.Handle.None, nearTarget);
        }

        /// <summary>
        /// Dragging a slack rope puts the drawn cord under the cursor, at the parameter the drag started
        /// from. Only slack ropes can promise this: a taut one draws the same line at every length, so
        /// there is nothing for the cord to track.
        /// </summary>
        [Theory]
        [InlineData(60)]
        [InlineData(140)]
        [InlineData(260)]
        public void SolvePutsTheCordUnderTheCursor(double dropBelowChord)
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "400"), Candy(200, 0));
            Assert.False(g.Taut);
            RopeLength.Drag drag = RopeLength.BeginDrag(g, g.KnobParameter, g.Knob);
            Vec2 cursor = new(100, dropBelowChord);

            double solved = RopeLength.Solve(g, drag, cursor);
            Vec2 cord = RopeStripBuilder.CalcPathBezier(
                RopeStripBuilder.ControlPoints(g.Hook, g.Target, solved), g.KnobParameter);

            // Not exact: the cord gains a control point every 35 units of rest length, so its shape steps
            // slightly and bisection lands inside a step. A few level units is sub-pixel at normal zoom.
            Assert.True(
                Math.Abs(cord.Y - cursor.Y) < 3,
                $"cord sat at Y {cord.Y} for a cursor at Y {cursor.Y}");
        }

        /// <summary>The solved length only ever grows as the cursor is pulled further from the chord.</summary>
        [Fact]
        public void SolveIsMonotoneInTheDragDistance()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "120"), Candy(200, 0));
            RopeLength.Drag drag = RopeLength.BeginDrag(g, 0.5, new Vec2(100, 0));

            double near = RopeLength.Solve(g, drag, new Vec2(100, 40));
            double middle = RopeLength.Solve(g, drag, new Vec2(100, 120));
            double far = RopeLength.Solve(g, drag, new Vec2(100, 300));

            Assert.True(near < middle);
            Assert.True(middle < far);
        }

        /// <summary>Dragging a slack rope back toward its chord bottoms out at taut, not below.</summary>
        [Fact]
        public void PlainDragBottomsOutAtTaut()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "400"), Candy(200, 0));
            RopeLength.Drag drag = RopeLength.BeginDrag(g, g.KnobParameter, g.Knob);

            Assert.Equal(g.Chord, RopeLength.Solve(g, drag, new Vec2(100, 0)), 0);
            Assert.Equal(g.Chord, RopeLength.Solve(g, drag, new Vec2(100, -400)), 0);
        }

        /// <summary>
        /// Pressing an already-taut rope leaves its length alone. Every length at or below the chord draws
        /// the same straight cord, so an absolute mapping would have to guess - and would guess the chord.
        /// </summary>
        [Theory]
        [InlineData("1")]
        [InlineData("50")]
        [InlineData("199")]
        [InlineData("200")]
        public void PressingATautRopeLeavesItsLengthAlone(string length)
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, length), Candy(200, 0));
            RopeLength.Drag drag = RopeLength.BeginDrag(g, g.KnobParameter, g.Knob);

            Assert.True(g.Taut);
            Assert.Equal(g.Length, RopeLength.Solve(g, drag, g.Knob), 6);
        }

        /// <summary>Dragging a taut rope grows it a step at a time from where it was, not from the chord.</summary>
        [Fact]
        public void DraggingATautRopeGrowsFromItsCurrentLength()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "50"), Candy(200, 0));
            RopeLength.Drag drag = RopeLength.BeginDrag(g, g.KnobParameter, g.Knob);

            double small = RopeLength.Solve(g, drag, new Vec2(100, 10));
            double bigger = RopeLength.Solve(g, drag, new Vec2(100, 40));

            Assert.True(small > 50, $"expected growth from 50, got {small}");
            Assert.True(bigger > small);
            Assert.True(small < 120, $"expected an incremental step from 50, got {small}");
        }

        /// <summary>The Alt mapping shifts the length by how far the drag moves, and floors at MinLength.</summary>
        [Fact]
        public void AltDragShortensRelativeToWhereItStarted()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "120"), Candy(200, 0));
            Vec2 origin = new(100, 0);
            RopeLength.Drag drag = RopeLength.BeginDrag(g, 0.5, origin);

            Assert.Equal(120, RopeLength.SolveTaut(g, drag, origin), 6);
            Assert.Equal(20, RopeLength.SolveTaut(g, drag, new Vec2(50, 0)), 6);
            Assert.Equal(220, RopeLength.SolveTaut(g, drag, new Vec2(150, 0)), 6);
            Assert.Equal(RopeLength.MinLength, RopeLength.SolveTaut(g, drag, new Vec2(0, 0)), 6);
        }

        /// <summary>Both mappings leave the length alone at the press point, so toggling Alt never jumps.</summary>
        [Fact]
        public void MappingsAgreeAtThePressPoint()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "120"), Candy(200, 0));
            Vec2 origin = new(100, 0);
            RopeLength.Drag drag = RopeLength.BeginDrag(g, 0.5, origin);

            Assert.Equal(120, RopeLength.Solve(g, drag, origin), 3);
            Assert.Equal(120, RopeLength.SolveTaut(g, drag, origin), 3);
        }

        /// <summary>A hook on its target has no chord to solve against, so the drag falls back to distance.</summary>
        [Fact]
        public void DegenerateChordSolvesByDistanceFromTheHook()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "200"), Candy(0, 0));
            RopeLength.Drag drag = RopeLength.BeginDrag(g, 0.5, new Vec2(0, 90));

            Assert.Equal(200, RopeLength.Solve(g, drag, new Vec2(0, 90)), 6);
            Assert.Equal(210, RopeLength.Solve(g, drag, new Vec2(0, 100)), 6);
        }

        /// <summary>Grabbing the knob and solving from where it already sits must not change the length.</summary>
        [Theory]
        [InlineData(200, 0)]
        [InlineData(200, 150)]
        [InlineData(0, 200)]
        public void PressingTheKnobLeavesTheLengthAlone(int candyX, int candyY)
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "400"), Candy(candyX, candyY));

            (RopeLength.Handle handle, double t) =
                RopeLength.HitTest(g, g.Knob, knobTolerance: 9, cordTolerance: 6);

            Assert.Equal(RopeLength.Handle.Knob, handle);
            Assert.Equal(g.Length, RopeLength.Solve(g, RopeLength.BeginDrag(g, t, g.Knob), g.Knob), 6);
        }

        /// <summary>Grabbing the cord away from the knob must not change the length either.</summary>
        [Fact]
        public void PressingTheCordLeavesTheLengthAlone()
        {
            RopeLength.Geometry g = Resolve(Grab(0, 0, "400"), Candy(200, 150));
            Vec2 onCord = RopeStripBuilder.CalcPathBezier(
                RopeStripBuilder.ControlPoints(g.Hook, g.Target, g.Length), 0.3);

            (RopeLength.Handle handle, double t) =
                RopeLength.HitTest(g, onCord, knobTolerance: 9, cordTolerance: 6);

            Assert.Equal(RopeLength.Handle.Cord, handle);
            Assert.Equal(g.Length, RopeLength.Solve(g, RopeLength.BeginDrag(g, t, onCord), onCord), 6);
        }
    }
}
