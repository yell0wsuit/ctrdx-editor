using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

using Avalonia;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Tests that a tutorial prompt's path is a real editable polyline on the canvas, and that the trigger
    /// area's corner-drag hit-testing wires up correctly. <c>EditablePath.For</c> keys only off
    /// <c>MoverPath.IsPolylineMovement(path)</c>, not object type, so nothing here should be tutorial-specific
    /// in production code - these tests exist to prove that claim rather than assume it, since the canvas
    /// itself can't be exercised visually in this environment.
    /// </summary>
    [Collection(LevelCanvasTestGroup.Name)]
    public class TutorialCanvasEditingTests
    {
        private static Type EditablePath =>
            typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.EditablePath")!;

        private static Type LevelSceneRenderer =>
            typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;

        /// <summary>A tutorial icon's real polyline movement is picked up by the canvas's generic path adapter,
        /// exactly as a mover's would be - <c>EditablePath.For</c> and <c>IsEditablePolyline</c> read only the
        /// <c>path</c> attribute, never the object's tag.</summary>
        [Fact]
        public void CanvasRecognizesATutorialPromptsPathAsEditable()
        {
            LevelObject prompt = Tutorial10("230,0,440,0", moveSpeed: "440", ease: "in,out");

            MethodInfo method = typeof(LevelCanvas).GetMethod(
                "IsEditablePolyline", BindingFlags.Static | BindingFlags.NonPublic)!;
            Assert.True((bool)method.Invoke(null, [prompt])!);

            object? path = For(prompt);
            Assert.NotNull(path);
            Assert.Equal(3, Points(path!).Length); // anchor + 2 offsets
        }

        /// <summary>Dragging a tutorial prompt's path point writes back to its <c>path</c> attribute, the same
        /// XML round-trip a mover's drag would produce - confirming the edit is real, not canvas-only.</summary>
        [Fact]
        public void MovingATutorialPromptsPathPointWritesTheAttribute()
        {
            LevelObject prompt = Tutorial10("230,0,440,0", moveSpeed: "440", ease: "in,out");
            object path = For(prompt)!;

            Invoke(path, "MovePoint", 1, new Vec2(200, 50));

            Assert.Equal("200,50,440,0", prompt.GetAttr("path"));
        }

        /// <summary>A tutorial text prompt (not an icon quad) is equally editable when it authors a real path.</summary>
        [Fact]
        public void TutorialTextPathIsAlsoEditable()
        {
            LevelObject text = new(new XElement(
                "tutorialText",
                new XAttribute("x", "10"),
                new XAttribute("y", "10"),
                new XAttribute("path", "50,0"),
                new XAttribute("moveSpeed", "100")));

            object? path = For(text);

            Assert.NotNull(path);
            Assert.Equal(2, Points(path).Length);
        }

        /// <summary>A prompt with no <c>inArea</c>, or one that fails to parse, offers no corner to grab.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("not,a,valid,area")]
        [InlineData("0,0,0,0")] // zero width/height fails TutorialArea.TryParse
        public void NoCornerHitsWithoutAParseableArea(string? inArea)
        {
            LevelCanvas canvas = new();
            LevelObject prompt = Tutorial05(inArea);
            canvas.SelectedObject = prompt;
            canvas.SelectedObjects = new HashSet<LevelObject> { prompt };

            int hit = HitTutorialAreaCorner(canvas, new Vec2(0, 0));

            Assert.Equal(-1, hit);
        }

        /// <summary>Each corner of a parseable area is hit-testable, and the interior is not - a point in the
        /// middle of the rectangle must miss so objects sitting inside a <c>candyMoved</c> region stay
        /// selectable through it.</summary>
        [Fact]
        public void OnlyTheFourCornersOfTheAreaAreHitTestable()
        {
            LevelCanvas canvas = new();
            LevelObject prompt = Tutorial05("100,100,50,50");
            canvas.SelectedObject = prompt;
            canvas.SelectedObjects = new HashSet<LevelObject> { prompt };

            Assert.Equal(0, HitTutorialAreaCorner(canvas, new Vec2(100, 100)));
            Assert.Equal(1, HitTutorialAreaCorner(canvas, new Vec2(150, 100)));
            Assert.Equal(2, HitTutorialAreaCorner(canvas, new Vec2(150, 150)));
            Assert.Equal(3, HitTutorialAreaCorner(canvas, new Vec2(100, 150)));
            Assert.Equal(-1, HitTutorialAreaCorner(canvas, new Vec2(125, 125))); // dead center of the rectangle
        }

        /// <summary>Dragging a fractional pointer writes integer area geometry that DX uses verbatim.</summary>
        [Fact]
        public void AreaCornerDragWritesWholeCoordinates()
        {
            LevelObject prompt = Tutorial05("100,100,50,50");

            ApplyTutorialAreaCornerDrag(prompt, 0, new Vec2(120.6, 110.4));

            Assert.Equal("121,110,29,40", prompt.GetAttr("inArea"));
        }

        /// <summary>A corner is only hit-testable while its prompt is the single selection, matching every
        /// other per-object canvas handle (rail, rope, conveyor, ...).</summary>
        [Fact]
        public void CornerIsNotHitTestableWhenItsPromptIsNotSelected()
        {
            // A prompt with a parseable area exists, but is never assigned to SelectedObject/SelectedObjects.
            LevelCanvas canvas = new();
            _ = Tutorial05("100,100,50,50");

            int hit = HitTutorialAreaCorner(canvas, new Vec2(100, 100));

            Assert.Equal(-1, hit);
        }

        /// <summary>
        /// The rendered path for a Timed tutorial prompt is identical whether <c>moveSpeed</c> is authored at
        /// all: <c>TutorialMotion.Timed</c> falls back to the game's own default of 100 either way, and that
        /// default affects only leg timing, never the path's own points. This is derived straight from the
        /// schema (the documented default), not from re-running the renderer's own logic.
        /// </summary>
        [Fact]
        public void TutorialMotionPathPointsAreIdenticalWhetherOrNotMoveSpeedIsAuthored()
        {
            LevelObject withoutSpeed = Tutorial10NoSpeed("230,0,440,0", ease: "in,out");
            LevelObject withSpeed100 = Tutorial10("230,0,440,0", moveSpeed: "100", ease: "in,out");

            Point[] pointsWithoutSpeed = ComputeTutorialMotionPathPoints(ViewTransform.Identity, withoutSpeed);
            Point[] pointsWithSpeed100 = ComputeTutorialMotionPathPoints(ViewTransform.Identity, withSpeed100);

            Assert.Equal(3, pointsWithoutSpeed.Length); // anchor + 2 offsets
            Assert.Equal(pointsWithSpeed100, pointsWithoutSpeed);
        }

        /// <summary>
        /// Pins the regression this task's review caught: a Timed tutorial prompt with no authored
        /// <c>moveSpeed</c> must still draw a real path. The shared mover's own point computation
        /// (<c>ComputeMovementPathPoints</c>, gated on <c>MoverPath.HasActiveMovement</c> - moveSpeed must be
        /// authored and positive) returns nothing at all for this exact object, which is the bug: the ease
        /// markers read straight from <c>TutorialMotion</c> regardless, so they would float with no path
        /// beneath them to sit on.
        /// </summary>
        [Fact]
        public void TutorialPathDrawsEvenWithoutAnAuthoredMoveSpeed()
        {
            LevelObject withoutSpeed = Tutorial10NoSpeed("230,0,440,0", ease: "in,out");

            Point[] tutorialPoints = ComputeTutorialMotionPathPoints(ViewTransform.Identity, withoutSpeed);
            Point[] sharedMoverPoints = ComputeMovementPathPoints(ViewTransform.Identity, withoutSpeed);

            Assert.True(tutorialPoints.Length >= 2); // a real, drawable path
            Assert.Empty(sharedMoverPoints); // confirms the shared mover gate really would have hidden it
        }

        private static int HitTutorialAreaCorner(LevelCanvas canvas, Vec2 levelPt)
        {
            MethodInfo method = typeof(LevelCanvas).GetMethod(
                "HitTutorialAreaCorner", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (int)method.Invoke(canvas, [levelPt])!;
        }

        private static void ApplyTutorialAreaCornerDrag(LevelObject prompt, int corner, Vec2 levelPt)
        {
            MethodInfo method = typeof(LevelCanvas).GetMethod(
                "ApplyTutorialAreaCornerDrag", BindingFlags.Static | BindingFlags.NonPublic)!;
            _ = method.Invoke(null, [prompt, corner, levelPt]);
        }

        private static object? For(LevelObject obj)
        {
            return EditablePath.GetMethod("For", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [obj]);
        }

        private static Vec2[] Points(object path)
        {
            return (Vec2[])path.GetType().GetProperty("Points")!.GetValue(path)!;
        }

        private static void Invoke(object path, string method, params object[] args)
        {
            _ = path.GetType().GetMethod(method)!.Invoke(path, args);
        }

        private static Point[] ComputeTutorialMotionPathPoints(ViewTransform v, LevelObject obj)
        {
            MethodInfo method = LevelSceneRenderer.GetMethod(
                "ComputeTutorialMotionPathPoints", BindingFlags.Public | BindingFlags.Static)!;
            return (Point[])method.Invoke(null, [v, obj])!;
        }

        private static Point[] ComputeMovementPathPoints(ViewTransform v, LevelObject obj)
        {
            MethodInfo method = LevelSceneRenderer.GetMethod(
                "ComputeMovementPathPoints", BindingFlags.Public | BindingFlags.Static)!;
            return (Point[])method.Invoke(null, [v, obj])!;
        }

        private static LevelObject Tutorial10(string path, string moveSpeed, string ease)
        {
            return new LevelObject(new XElement(
                "tutorial10",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("path", path),
                new XAttribute("moveSpeed", moveSpeed),
                new XAttribute("ease", ease)));
        }

        /// <summary>A Timed tutorial prompt that deliberately omits <c>moveSpeed</c> - the case the game
        /// still animates at its own default speed, but the shared mover's active-movement gate would treat
        /// as motionless.</summary>
        private static LevelObject Tutorial10NoSpeed(string path, string ease)
        {
            return new LevelObject(new XElement(
                "tutorial10",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("path", path),
                new XAttribute("ease", ease)));
        }

        private static LevelObject Tutorial05(string? inArea)
        {
            XElement element = new(
                "tutorial05",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"));
            if (inArea is not null)
            {
                element.SetAttributeValue("inArea", inArea);
            }

            return new LevelObject(element);
        }
    }
}
