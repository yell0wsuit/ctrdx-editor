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
                PlaceAt = (_, x, y) => Placement.CreateObject(DescriptorTable.Default.For("star")!, x, y),
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
                PlaceAt = (_, x, y) => Placement.CreateObject(DescriptorTable.Default.For("star")!, x, y),
            };

            bool placed = canvas.DropElement("star", new Point(10, 20));

            Assert.True(placed);
        }
    }
}
