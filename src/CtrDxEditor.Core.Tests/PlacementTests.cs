using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for creating XML-backed objects from descriptors.</summary>
    public class PlacementTests
    {
        /// <summary>Verifies that placement writes coordinates and descriptor defaults.</summary>
        [Fact]
        public void CreateObjectSetsCoordinatesAndDefaults()
        {
            ObjectDescriptor star = DescriptorTable.CtrObjects.For("star")!;

            LevelObject obj = Placement.CreateObject(star, x: 40, y: 60);

            Assert.Equal("star", obj.Type);
            Assert.Equal(40, obj.X);
            Assert.Equal(60, obj.Y);
            Assert.Equal("-1", obj.GetAttr("timeout"));
        }

        /// <summary>Verifies that attributes without defaults are omitted from new objects.</summary>
        [Fact]
        public void CreateObjectSkipsAttributesWithoutADefault()
        {
            ObjectDescriptor grab = DescriptorTable.CtrObjects.For("grab")!;

            LevelObject obj = Placement.CreateObject(grab, x: 1, y: 2);

            Assert.Equal("100", obj.GetAttr("length"));
            Assert.Null(obj.GetAttr("part"));
        }

        /// <summary>Verifies electro placement writes the exact game timing defaults.</summary>
        [Fact]
        public void CreateElectroObjectSetsTimingDefaults()
        {
            ObjectDescriptor electro = DescriptorTable.CtrObjects.For("electro")!;

            LevelObject obj = Placement.CreateObject(electro, x: 250, y: 186);

            Assert.Equal("electro", obj.Type);
            Assert.Equal("0.0", obj.GetAttr("initialDelay"));
            Assert.Equal("2.0", obj.GetAttr("offTime"));
            Assert.Equal("2.0", obj.GetAttr("onTime"));
            Assert.Equal("0", obj.GetAttr("angle"));
            Assert.Equal("5", obj.GetAttr("size"));
        }

        /// <summary>Verifies magic-hat placement writes the DX sock element and default teleport group.</summary>
        [Fact]
        public void CreateSockObjectSetsTeleportGroupDefault()
        {
            ObjectDescriptor sock = DescriptorTable.CtrObjects.For("sock")!;

            LevelObject obj = Placement.CreateObject(sock, x: 120, y: 240);

            Assert.Equal("sock", obj.Type);
            Assert.Equal(120, obj.X);
            Assert.Equal(240, obj.Y);
            Assert.Equal("0", obj.GetAttr("group"));
            Assert.Equal("0", obj.GetAttr("angle"));
        }

        /// <summary>A newly placed mechanical hand seeds its first live segment with a long, editable arm.</summary>
        [Fact]
        public void CreateHandObjectSeedsFirstSegment()
        {
            ObjectDescriptor hand = DescriptorTable.CtrObjects.For("hand")!;

            LevelObject obj = Placement.CreateObject(hand, x: 162, y: 254);

            Assert.Equal("1", obj.GetAttr("segmentsCount"));
            Assert.Equal("0", obj.GetAttr("segment1Angle"));
            Assert.Equal("50", obj.GetAttr("segment1Length"));
            Assert.Equal("true", obj.GetAttr("segment1Rotatable"));
        }
    }
}
