using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the drag→readout resolver that feeds the canvas's live value badges.</summary>
    public class DragReadoutTests
    {
        private static LevelObject Obj(string type, params (string Name, string Value)[] attrs)
        {
            XElement e = new(type, new XAttribute("x", "10"), new XAttribute("y", "20"));
            foreach ((string name, string value) in attrs)
            {
                e.SetAttributeValue(name, value);
            }
            return new LevelObject(e);
        }

        private static IReadOnlyList<DragReadout.Entry> For(
            DragKind kind, LevelObject? obj = null, int index = 0, Vec2 point = default)
        {
            return DragReadout.For(kind, obj, index, point);
        }

        /// <summary>A move readout reports the object's own X and Y, in that order.</summary>
        [Fact]
        public void MoveReportsXThenY()
        {
            IReadOnlyList<DragReadout.Entry> entries = For(DragKind.Move, Obj("grab"));

            Assert.Equal(
                [new DragReadout.Entry("x", "10"), new DragReadout.Entry("y", "20")],
                entries);
        }

        /// <summary>A hand base drag reports position, same as a plain move.</summary>
        [Fact]
        public void HandBaseReportsPosition()
        {
            IReadOnlyList<DragReadout.Entry> entries = For(DragKind.HandBase, Obj("hand"));

            Assert.Equal(
                [new DragReadout.Entry("x", "10"), new DragReadout.Entry("y", "20")],
                entries);
        }

        /// <summary>
        /// The badge reports the same number the property panel does. The panel binds the raw <c>angle</c>
        /// attribute, so a spec's DisplayOffset or StoredAngleSign must not leak into the readout — a pump
        /// storing 0 reads 0, not the 90 it renders at.
        /// </summary>
        [Theory]
        [InlineData("pump", "0", "0°")]
        [InlineData("pump", "45", "45°")]
        [InlineData("rocket", "20", "20°")]
        [InlineData("transporter", "45", "45°")]
        [InlineData("bouncer1", "-30", "-30°")]
        public void RotationMatchesThePropertyPanelValue(string type, string stored, string expected)
        {
            DragReadout.Entry entry = Assert.Single(
                For(DragKind.Rotate, Obj(type, ("angle", stored))));

            Assert.Equal("angle", entry.AttrKey);
            Assert.Equal(expected, entry.Value);
        }

        /// <summary>A rotation readout carries the degree sign, so the number is not read as a length.</summary>
        [Fact]
        public void RotateCarriesDegreeSign()
        {
            DragReadout.Entry entry = Assert.Single(
                For(DragKind.Rotate, Obj("bouncer1", ("angle", "45"))));

            Assert.Equal("angle", entry.AttrKey);
            Assert.EndsWith("°", entry.Value, StringComparison.Ordinal);
        }

        /// <summary>A hand rotation reads the active segment's synthesized angle property.</summary>
        [Fact]
        public void HandRotationReadsTheActiveSegment()
        {
            LevelObject hand = Obj(
                "hand",
                ("segmentsCount", "2"),
                ("segment1Angle", "15"),
                ("segment2Angle", "90"));

            DragReadout.Entry entry = Assert.Single(For(DragKind.Rotate, hand, index: 2));

            Assert.Equal("angle", entry.AttrKey);
            Assert.Equal("90°", entry.Value);
        }

        /// <summary>A ghost's bouncer preview rotates through its ordinary angle attribute.</summary>
        [Fact]
        public void GhostRotationReadsItsPreviewAngle()
        {
            DragReadout.Entry entry = Assert.Single(
                For(DragKind.Rotate, Obj("ghost", ("angle", "45"))));

            Assert.Equal("angle", entry.AttrKey);
            Assert.Equal("45°", entry.Value);
        }

        /// <summary>A radius readout uses the ring's own attribute, not a hard-coded "radius".</summary>
        [Fact]
        public void RadiusUsesTheRingsOwnAttribute()
        {
            DragReadout.Entry entry = Assert.Single(
                For(DragKind.Radius, Obj("lightBulb", ("litRadius", "150"))));

            Assert.Equal("litRadius", entry.AttrKey);
            Assert.Equal("150", entry.Value);
        }

        /// <summary>A ghost's grab preview resizes the ghost's own catch radius.</summary>
        [Fact]
        public void GhostRadiusReadsItsPreviewRadius()
        {
            DragReadout.Entry entry = Assert.Single(
                For(DragKind.Radius, Obj("ghost", ("radius", "175"))));

            Assert.Equal("radius", entry.AttrKey);
            Assert.Equal("175", entry.Value);
        }

        /// <summary>A rail resize reports both attributes the drag writes, offset first.</summary>
        [Fact]
        public void RailResizeReportsOffsetThenLength()
        {
            IReadOnlyList<DragReadout.Entry> entries = For(
                DragKind.RailResize, Obj("grab", ("moveOffset", "12"), ("moveLength", "340")));

            Assert.Equal(
                [new DragReadout.Entry("moveOffset", "12"), new DragReadout.Entry("moveLength", "340")],
                entries);
        }

        /// <summary>Sliding the hook on a horizontal rail reports the offset and the X it moved.</summary>
        [Fact]
        public void RailOffsetReportsTheMovedAxis()
        {
            IReadOnlyList<DragReadout.Entry> horizontal = For(
                DragKind.RailOffset, Obj("grab", ("moveOffset", "12"), ("moveVertical", "false")));
            IReadOnlyList<DragReadout.Entry> vertical = For(
                DragKind.RailOffset, Obj("grab", ("moveOffset", "12"), ("moveVertical", "true")));

            Assert.Equal("x", horizontal[1].AttrKey);
            Assert.Equal("10", horizontal[1].Value);
            Assert.Equal("y", vertical[1].AttrKey);
            Assert.Equal("20", vertical[1].Value);
        }

        /// <summary>A hand joint drag reads the segment named by index, not the first one.</summary>
        [Fact]
        public void HandJointReadsTheIndexedSegment()
        {
            LevelObject hand = Obj(
                "hand",
                ("segmentsCount", "2"),
                ("segment1Length", "40"),
                ("segment2Length", "90"));

            DragReadout.Entry entry = Assert.Single(For(DragKind.HandJoint, hand, index: 2));

            Assert.Equal("length", entry.AttrKey);
            Assert.Equal("90", entry.Value);
        }

        /// <summary>A polyline vertex has no attribute of its own, so the canvas supplies the point.</summary>
        [Fact]
        public void PolylinePointUsesTheSuppliedPoint()
        {
            IReadOnlyList<DragReadout.Entry> entries = For(
                DragKind.PolylinePoint, point: new Vec2(300, 450));

            Assert.Equal(
                [new DragReadout.Entry("x", "300"), new DragReadout.Entry("y", "450")],
                entries);
        }

        /// <summary>Water is level-wide, so it resolves from the supplied height with no object at all.</summary>
        [Fact]
        public void WaterUsesTheSuppliedHeightWithNoObject()
        {
            DragReadout.Entry entry = Assert.Single(For(DragKind.Water, point: new Vec2(0, 512)));

            Assert.Equal("water", entry.AttrKey);
            Assert.Equal("512", entry.Value);
        }

        /// <summary>The conveyor's two handles write different attributes and must not be conflated.</summary>
        [Fact]
        public void ConveyorHandlesReportDifferentAttributes()
        {
            LevelObject belt = Obj("transporter", ("length", "200"), ("width", "40"));

            Assert.Equal("length", Assert.Single(For(DragKind.ConveyorLength, belt)).AttrKey);
            Assert.Equal("width", Assert.Single(For(DragKind.ConveyorWidth, belt)).AttrKey);
        }

        /// <summary>A strip resize reports its discrete size class.</summary>
        [Fact]
        public void StripSizeReportsTheSizeClass()
        {
            DragReadout.Entry entry = Assert.Single(
                For(DragKind.StripSize, Obj("spike3", ("size", "3"))));

            Assert.Equal("size", entry.AttrKey);
            Assert.Equal("3", entry.Value);
        }

        /// <summary>Inconsistent state yields no entries rather than throwing mid-render.</summary>
        [Theory]
        [InlineData(DragKind.None)]
        [InlineData(DragKind.Move)]
        [InlineData(DragKind.Rotate)]
        [InlineData(DragKind.RopeLength)]
        public void InconsistentStateYieldsNothing(DragKind kind)
        {
            Assert.Empty(For(kind));
        }

        /// <summary>No drag yields nothing even when a stale selection object is still present.</summary>
        [Fact]
        public void NoneWithAnObjectYieldsNothing()
        {
            Assert.Empty(For(DragKind.None, Obj("grab")));
        }

        /// <summary>An out-of-range hand segment yields nothing instead of throwing.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        public void OutOfRangeHandSegmentYieldsNothing(int index)
        {
            LevelObject hand = Obj(
                "hand",
                ("segmentsCount", "2"),
                ("segment1Length", "40"),
                ("segment2Length", "90"));

            Assert.Empty(For(DragKind.HandJoint, hand, index));
        }

        /// <summary>
        /// Every attribute key the resolver can emit has an "Attr.*" entry in en.json. Without this,
        /// a missing key silently renders as the raw XML name ("moveOffset") in the badge.
        /// </summary>
        [Fact]
        public void EveryEmittableKeyIsLocalized()
        {
            Dictionary<string, string> strings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(LocalizationPath()))!;

            string[] keys =
            [
                "x", "y", "water", "angle", "radius", "litRadius", "length", "width",
                "size", "handleAngle", "moveOffset", "moveLength",
            ];

            foreach (string key in keys)
            {
                Assert.True(strings.ContainsKey($"Attr.{key}"), $"en.json is missing Attr.{key}");
            }
        }

        private static string LocalizationPath()
        {
            string path = AppContext.BaseDirectory;
            while (!Directory.Exists(Path.Combine(path, "resources")))
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate the repository root.");
            }

            return Path.Combine(path, "resources", "localization", "en.json");
        }
    }
}
