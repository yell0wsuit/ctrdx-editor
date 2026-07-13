using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Canvas resize geometry for tutorial text wrap width.</summary>
    public class TutorialTextResizeTests
    {
        /// <summary>Only hits the knob at the right-edge midpoint of the text box.</summary>
        [Fact]
        public void HitTestFindsRightEdgeHandle()
        {
            LevelBounds bounds = new(10, 20, 100, 40);

            Assert.True(TutorialTextResize.HitTest(bounds, new Vec2(110, 40), 4));
            Assert.False(TutorialTextResize.HitTest(bounds, new Vec2(100, 40), 4));
        }

        /// <summary>Dragging preserves where within the wider hit target the handle was grabbed.</summary>
        [Fact]
        public void EdgeFromPointerPreservesGrabOffset()
        {
            LevelBounds bounds = new(10, 20, 100, 40);
            double grabOffset = TutorialTextResize.GrabOffset(bounds, pointerX: 106);

            Assert.Equal(130, TutorialTextResize.EdgeFromPointer(pointerX: 126, grabOffset));
        }

        /// <summary>A click or tiny pointer jitter does not begin authoring a manual width.</summary>
        [Theory]
        [InlineData(100, 100, 1, false)]
        [InlineData(100, 101, 1, false)]
        [InlineData(100, 103, 1, true)]
        [InlineData(100, 101, 2, true)]
        public void HasDraggedUsesScreenSpaceThreshold(
            double startPointerX,
            double pointerX,
            double zoom,
            bool expected)
        {
            Assert.Equal(expected, TutorialTextResize.HasDragged(startPointerX, pointerX, zoom));
        }

        /// <summary>Once resizing starts, returning inside the initial threshold still follows the pointer.</summary>
        [Fact]
        public void ShouldApplyDragRemainsTrueAfterThresholdWasCrossed()
        {
            Assert.True(TutorialTextResize.ShouldApplyDrag(
                hasDragged: true,
                startPointerX: 100,
                pointerX: 101,
                zoom: 1));
        }

        /// <summary>Dragging writes the game width and changes auto-width to manual editor state.</summary>
        [Fact]
        public void ApplyDragWritesWidthAndDisablesAutoWidth()
        {
            LevelObject text = new(new XElement(
                "tutorialText",
                new XAttribute("x", "10"),
                new XAttribute("y", "20"),
                new XAttribute("width", "40")));
            TutorialObject.SetAutoWidth(text, true);

            TutorialTextResize.ApplyDrag(text, 75);

            Assert.Equal("65", text.GetAttr("width"));
            Assert.False(TutorialObject.IsAutoWidth(text));
            Assert.Null(text.GetAttr("autoWidth"));
        }

        /// <summary>Prevents a drag left of the origin from producing zero or negative wrap width.</summary>
        [Fact]
        public void ApplyDragClampsToMinimumWidth()
        {
            LevelObject text = new(new XElement(
                "tutorialText",
                new XAttribute("x", "10"),
                new XAttribute("width", "40")));

            TutorialTextResize.ApplyDrag(text, -100);

            Assert.Equal("16", text.GetAttr("width"));
        }
    }
}
