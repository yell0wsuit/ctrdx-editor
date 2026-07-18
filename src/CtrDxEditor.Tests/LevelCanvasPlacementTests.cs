using Avalonia;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests palette placement behavior on the editor canvas.</summary>
    public class LevelCanvasPlacementTests
    {
        /// <summary>Verifies click placement reports success so the host can return keyboard focus to the canvas.</summary>
        [Fact]
        public void AddAtCenterReturnsTrueWhenObjectIsPlaced()
        {
            LevelCanvas canvas = new()
            {
                Document = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, false, false)),
                PlaceAt = (_, x, y) => Placement.CreateObject(DescriptorTable.CtrObjects.For("star")!, x, y),
            };

            bool placed = canvas.AddAtCenter("star");

            Assert.True(placed);
        }

        /// <summary>Verifies drag placement reports success so the host can return keyboard focus to the canvas.</summary>
        [Fact]
        public void DropElementReturnsTrueWhenObjectIsPlaced()
        {
            LevelCanvas canvas = new()
            {
                Document = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, false, false)),
                View = ViewTransform.Identity,
                PlaceAt = (_, x, y) => Placement.CreateObject(DescriptorTable.CtrObjects.For("star")!, x, y),
            };

            bool placed = canvas.DropElement("star", new Point(10, 20));

            Assert.True(placed);
        }

        /// <summary>Adding an ant conveyor from the palette carries the editable path defaults onto the canvas.</summary>
        [Fact]
        public void AddAtCenterPlacesAntConveyorWithPathDefaults()
        {
            LevelObject? created = null;
            LevelCanvas canvas = new()
            {
                Document = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, false, false)),
                PlaceAt = (_, x, y) => created = Placement.CreateObject(
                    DescriptorTable.CtrObjects.For(AntPath.Element)!, x, y),
            };

            Assert.True(canvas.AddAtCenter(AntPath.Element));
            Assert.NotNull(created);
            Assert.Equal(AntPath.DefaultPath, created!.GetAttr("path"));
            Assert.Equal(AntPath.DefaultMoveSpeed, created.GetAttr("moveSpeed"));
        }
    }
}
