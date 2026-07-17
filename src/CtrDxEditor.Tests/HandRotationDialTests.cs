using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests rotation-dial target resolution for active hand segments and ordinary objects.</summary>
    public class HandRotationDialTests
    {
        /// <summary>An active hand segment resolves its indexed angle and its starting joint as the pivot.</summary>
        [Fact]
        public void HandRotationDialResolvesActiveSegment()
        {
            LevelObject hand = Hand();

            RotationDialTarget? target = RotationDialTargetResolver.Resolve(hand, activeHandSegment: 2, ordinarySpec: null);

            RotationDialTarget resolved = Assert.NotNull(target);
            Assert.Equal(2, resolved.HandSegmentIndex);
            Assert.Equal("segment2Angle", resolved.Spec.AttributeName);
            Assert.Equal(150, resolved.Center.X, 6);
            Assert.Equal(200, resolved.Center.Y, 6);
            Assert.Equal(90, resolved.StoredAngle);
        }

        /// <summary>Applying a hand dial angle writes only that segment through the whole-degree hand writer.</summary>
        [Fact]
        public void HandRotationDialWritesOnlyActiveAngle()
        {
            LevelObject hand = Hand();
            RotationDialTarget target = RotationDialTargetResolver.Resolve(hand, 2, null)!.Value;

            RotationDialTargetResolver.ApplyAngle(hand, target, 44.6, target.Center);

            Assert.Equal("0", hand.GetAttr("segment1Angle"));
            Assert.Equal("45", hand.GetAttr("segment2Angle"));
            Assert.Equal("50", hand.GetAttr("segment1Length"));
            Assert.Equal("40", hand.GetAttr("segment2Length"));
        }

        /// <summary>Invalid hand segment state clamps after deletion and clears for another object type.</summary>
        [Fact]
        public void HandRotationDialClampsOrClearsInvalidState()
        {
            LevelObject hand = Hand();

            Assert.Equal(2, RotationDialTargetResolver.ClampActiveHandSegment(hand, 3));
            HandObject.DeleteSegment(hand, 2);
            Assert.Equal(1, RotationDialTargetResolver.ClampActiveHandSegment(hand, 2));

            LevelObject pump = new(new XElement("pump", new XAttribute("x", "10"), new XAttribute("y", "20")));
            Assert.Equal(0, RotationDialTargetResolver.ClampActiveHandSegment(pump, 1));
        }

        /// <summary>Ordinary rotation targets retain their registered spec, stored angle, and object pivot.</summary>
        [Fact]
        public void HandRotationDialResolverPreservesOrdinaryObjects()
        {
            LevelObject pump = new(new XElement(
                "pump",
                new XAttribute("x", "10"),
                new XAttribute("y", "20"),
                new XAttribute("angle", "30")));
            RotationSpec spec = RotationTable.EditableFor("pump")!;

            RotationDialTarget? target = RotationDialTargetResolver.Resolve(pump, 0, spec);

            RotationDialTarget resolved = Assert.NotNull(target);
            Assert.Equal(0, resolved.HandSegmentIndex);
            Assert.Same(spec, resolved.Spec);
            Assert.Equal(10, resolved.Center.X, 6);
            Assert.Equal(20, resolved.Center.Y, 6);
            Assert.Equal(30, resolved.StoredAngle);
        }

        private static LevelObject Hand()
        {
            return new LevelObject(new XElement(
                "hand",
                new XAttribute("x", "100"),
                new XAttribute("y", "200"),
                new XAttribute("segmentsCount", "2"),
                new XAttribute("segment1Angle", "0"),
                new XAttribute("segment1Length", "50"),
                new XAttribute("segment1Rotatable", "true"),
                new XAttribute("segment2Angle", "90"),
                new XAttribute("segment2Length", "40"),
                new XAttribute("segment2Rotatable", "false")));
        }
    }
}
