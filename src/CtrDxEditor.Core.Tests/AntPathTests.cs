using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for ant-conveyor path semantics and editing.</summary>
    public class AntPathTests
    {
        /// <summary>A terminal anchor offset is the format's explicit loop marker.</summary>
        [Theory]
        [InlineData("64,-40,130,-76,201,-75,", false)]
        [InlineData("41,-15,90,-17,0,0,", true)]
        public void DetectsExplicitClosure(string path, bool closed)
        {
            Assert.Equal(closed, AntPath.IsClosed(path));
        }

        /// <summary>The duplicate terminal anchor is semantic closure, not a second editable handle.</summary>
        [Fact]
        public void PointsExcludeSyntheticClosingAnchor()
        {
            LevelObject ants = Ant("100,0,100,100,0,0", x: 10, y: 20);

            Assert.Equal(
                [new Vec2(10, 20), new Vec2(110, 20), new Vec2(110, 120)],
                AntPath.Points(ants));
        }

        /// <summary>Moving an ordinary vertex retains the explicit closing marker.</summary>
        [Fact]
        public void MovingVertexPreservesClosedLoop()
        {
            LevelObject ants = Ant("100,0,100,100,0,0", x: 0, y: 0);

            AntPath.MovePoint(ants, 1, new Vec2(120, 10));

            Assert.Equal("120,10,100,100,0,0", ants.GetAttr("path"));
        }

        /// <summary>Insertion and append place new vertices before a closed path's terminal anchor.</summary>
        [Fact]
        public void InsertAndAppendPreserveTerminalClosure()
        {
            LevelObject ants = Ant("100,0,100,100,0,0", x: 0, y: 0);

            AntPath.InsertPoint(ants, 2, new Vec2(50, 100));
            AntPath.AppendPoint(ants, new Vec2(0, 50));

            Assert.Equal("100,0,100,100,50,100,0,50,0,0", ants.GetAttr("path"));
        }

        /// <summary>Canvas deletion cannot remove the final non-anchor endpoint.</summary>
        [Fact]
        public void DeleteKeepsAtLeastOneDistinctEndpoint()
        {
            LevelObject open = Ant("100,0", x: 0, y: 0);
            LevelObject closed = Ant("100,0,0,0", x: 0, y: 0);

            AntPath.DeletePoint(open, 1);
            AntPath.DeletePoint(closed, 1);

            Assert.Equal("100,0", open.GetAttr("path"));
            Assert.Equal("100,0,0,0", closed.GetAttr("path"));
        }

        /// <summary>The semantic closed property adds or removes only one terminal anchor.</summary>
        [Fact]
        public void SetClosedOnlyChangesTerminalAnchor()
        {
            LevelObject ants = Ant("100,0", x: 0, y: 0);

            AntPath.SetClosed(ants, true);
            Assert.Equal("100,0,0,0", ants.GetAttr("path"));

            AntPath.SetClosed(ants, false);
            Assert.Equal("100,0", ants.GetAttr("path"));
        }

        /// <summary>Bounds include every authored vertex plus the requested padding.</summary>
        [Fact]
        public void BoundsCoverCompletePath()
        {
            LevelObject ants = Ant("100,0,100,80");

            Assert.Equal(new LevelBounds(4, 14, 132, 112), AntPath.Bounds(ants));
        }

        private static LevelObject Ant(string path, int x = 20, int y = 30)
        {
            return new LevelObject(new XElement(AntPath.Element,
                new XAttribute("x", x),
                new XAttribute("y", y),
                new XAttribute("path", path),
                new XAttribute("moveSpeed", AntPath.DefaultMoveSpeed)));
        }
    }
}
