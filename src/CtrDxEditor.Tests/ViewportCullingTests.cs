using System;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

using Avalonia;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the renderer's viewport cull predicate, which decides whether an object is drawn.</summary>
    public class ViewportCullingTests
    {
        private static readonly Type SceneRenderer =
            typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;

        private static bool IsWithinViewport(LevelBounds bounds, ViewTransform view, Size renderSize, double margin)
        {
            MethodInfo method = SceneRenderer.GetMethod(
                "IsWithinViewport", BindingFlags.Public | BindingFlags.Static)!;
            return (bool)method.Invoke(null, [bounds, view, renderSize, margin])!;
        }

        /// <summary>An object sitting in the middle of the viewport is drawn.</summary>
        [Fact]
        public void ObjectInsideViewportIsKept()
        {
            LevelBounds bounds = new(100, 100, 50, 50);
            ViewTransform view = new(1.0, 0, 0);

            Assert.True(IsWithinViewport(bounds, view, new Size(800, 600), 0));
        }

        /// <summary>An object well past the right edge is culled.</summary>
        [Fact]
        public void ObjectBeyondRightEdgeIsCulled()
        {
            LevelBounds bounds = new(5000, 100, 50, 50);
            ViewTransform view = new(1.0, 0, 0);

            Assert.False(IsWithinViewport(bounds, view, new Size(800, 600), 0));
        }

        /// <summary>An object past the edge but inside the margin is kept, so overhanging art is not clipped.</summary>
        [Fact]
        public void ObjectWithinMarginIsKept()
        {
            LevelBounds bounds = new(850, 100, 50, 50);
            ViewTransform view = new(1.0, 0, 0);

            Assert.False(IsWithinViewport(bounds, view, new Size(800, 600), 0));
            Assert.True(IsWithinViewport(bounds, view, new Size(800, 600), 256));
        }

        /// <summary>An object straddling an edge is kept, so partially visible art still draws.</summary>
        [Fact]
        public void ObjectStraddlingEdgeIsKept()
        {
            LevelBounds bounds = new(-25, 100, 50, 50);
            ViewTransform view = new(1.0, 0, 0);

            Assert.True(IsWithinViewport(bounds, view, new Size(800, 600), 0));
        }

        /// <summary>Pan and zoom are respected: the same object leaves the viewport once panned away.</summary>
        [Fact]
        public void PanAndZoomMoveTheCullBoundary()
        {
            LevelBounds bounds = new(100, 100, 50, 50);

            Assert.True(IsWithinViewport(bounds, new(1.0, 0, 0), new Size(800, 600), 0));
            Assert.False(IsWithinViewport(bounds, new(1.0, -2000, 0), new Size(800, 600), 0));
            Assert.True(IsWithinViewport(bounds, new(4.0, 0, 0), new Size(800, 600), 0));
            Assert.False(IsWithinViewport(bounds, new(8.0, 0, 0), new Size(800, 600), 0));
        }

        /// <summary>The cull margin is generous enough to cover the widest overhanging art.</summary>
        [Fact]
        public void CullMarginIsAtLeastTwoFiftySix()
        {
            FieldInfo field = typeof(SpriteCache).Assembly
                .GetType("CtrDxEditor.Rendering.LevelCanvas")!
                .GetField("CullMargin", BindingFlags.NonPublic | BindingFlags.Static)!;

            Assert.True((double)field.GetRawConstantValue()! >= 256);
        }

        /// <summary>
        /// End-to-end cull decision over real objects: bounds come from <c>SelectionBounds</c> exactly as the
        /// draw loop computes them, so this covers the pairing the renderer actually relies on rather than the
        /// predicate in isolation.
        /// </summary>
        [Fact]
        public void CullKeepsOnlyObjectsNearTheViewport()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            ViewTransform view = new(1.0, 0, 0);
            Size renderSize = new(800, 600);

            // Without art, SelectionBounds falls back to a 16x16 box centered on the object, which is all the
            // cull decision needs — it is the geometry, not the pixels, under test here.
            Assert.True(Kept(sprites, Obj(400, 300), view, renderSize));
            Assert.True(Kept(sprites, Obj(0, 0), view, renderSize));
            Assert.True(Kept(sprites, Obj(800, 600), view, renderSize));

            // Inside the margin: kept, so overhanging art never pops at the edge.
            Assert.True(Kept(sprites, Obj(1000, 300), view, renderSize));

            // Well beyond the margin: culled.
            Assert.False(Kept(sprites, Obj(5000, 300), view, renderSize));
            Assert.False(Kept(sprites, Obj(400, -3000), view, renderSize));
        }

        /// <summary>
        /// A mover authored far offscreen is drawn once live preview carries it into the viewport. Culling on the
        /// authored position instead would erase it for the whole pass it spends on screen.
        /// </summary>
        [Fact]
        public void MoverAnimatedIntoTheViewportIsKept()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            ViewTransform view = new(1.0, 0, 0);
            Size renderSize = new(800, 600);

            // Authored ~7000 units past the right edge, sweeping left at 100 units/s. The leg is far longer than
            // the 7400 units covered by t=74s, so the object is still on its first pass: x = 7750 - 7400 = 350.
            LevelObject mover = Mover("star", 7750, 150, "-9999,0", moveSpeed: 100);

            Assert.False(Kept(sprites, mover, view, renderSize));
            Assert.True(Kept(sprites, mover, view, renderSize, previewSeconds: 74));
        }

        /// <summary>A mover authored on screen is culled once live preview carries it away, so the cull still pays off.</summary>
        [Fact]
        public void MoverAnimatedOutOfTheViewportIsCulled()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            ViewTransform view = new(1.0, 0, 0);
            Size renderSize = new(800, 600);

            // Authored mid-viewport, climbing out of the top at 100 units/s on a leg long enough that t=74s is
            // still on the first pass: y = 300 - 7400 = -7100, far beyond the margin.
            LevelObject mover = Mover("bouncer1", 400, 300, "0,-10734", moveSpeed: 100);

            Assert.True(Kept(sprites, mover, view, renderSize));
            Assert.False(Kept(sprites, mover, view, renderSize, previewSeconds: 74));
        }

        /// <summary>
        /// An ant conveyor keeps its authored bounds under preview. Ants ship with a path and move speed by
        /// default, but <c>AntRenderer</c> lays the trail along the authored path and only marches the ant
        /// sprites down it, so displacing the cull box would erase a conveyor sitting in plain view.
        /// </summary>
        [Fact]
        public void AntConveyorKeepsItsAuthoredBoundsUnderPreview()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            ViewTransform view = new(1.0, 0, 0);
            Size renderSize = new(800, 600);

            // The trail runs from the on-screen anchor out to x=4400. At 100 units/s, t=40s is exactly the far
            // end, so displacing the box would move it to 4400..8400 — wholly outside the margin, and culled,
            // even though the trail itself never moved off screen.
            LevelObject ants = Mover(AntPath.Element, 400, 300, "4000,0", moveSpeed: 100);

            Assert.True(Kept(sprites, ants, view, renderSize));
            Assert.True(Kept(sprites, ants, view, renderSize, previewSeconds: 40));
        }

        /// <summary>Types drawn from authored geometry are excluded from preview displacement; movers are not.</summary>
        /// <remarks>
        /// This locks the list against <c>DrawObject</c>'s early returns. A new branch there that draws without
        /// applying the preview position must be added here too, or culling silently drops it during playback.
        /// </remarks>
        [Theory]
        [InlineData("star", true)]
        [InlineData("bouncer1", true)]
        [InlineData("candy", true)]
        [InlineData("gap", true)]
        [InlineData("ants", false)]
        [InlineData("transporter", false)]
        [InlineData("hand", false)]
        [InlineData("tutorialText", false)]
        [InlineData("tutorial01", false)]
        public void DrawsAtPreviewPositionMatchesTheDrawBranches(string type, bool expected)
        {
            MethodInfo method = SceneRenderer.GetMethod(
                "DrawsAtPreviewPosition", BindingFlags.Public | BindingFlags.Static)!;
            LevelObject obj = Obj(type, 400, 300);

            Assert.Equal(expected, (bool)method.Invoke(null, [obj])!);
        }

        /// <summary>A mover with preview off keeps its authored bounds, so a stopped preview renders as authored.</summary>
        [Fact]
        public void MoverWithPreviewOffKeepsAuthoredBounds()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            ViewTransform view = new(1.0, 0, 0);
            Size renderSize = new(800, 600);

            LevelObject mover = Mover("star", 5000, 300, "-9999,0", moveSpeed: 100);

            Assert.False(Kept(sprites, mover, view, renderSize));
        }

        /// <summary>A circular orbit path displaces the cull box too, covering the other mover flavour.</summary>
        [Fact]
        public void CircularMoverIsCulledOnItsOrbitPosition()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            ViewTransform view = new(1.0, 0, 0);
            Size renderSize = new(800, 600);

            // A circular path treats the authored point as the orbit centre, so the object starts a full radius
            // away at (2400, 300) — past the 1056px margin edge, and culled the moment preview begins.
            LevelObject orbiter = Mover("star", 400, 300, "RC2000", moveSpeed: 300);

            Assert.True(Kept(sprites, orbiter, view, renderSize));
            Assert.False(Kept(sprites, orbiter, view, renderSize, previewSeconds: 0.001));
        }

        /// <summary>A static object ignores preview time, so the overwhelmingly common case keeps its authored bounds.</summary>
        [Fact]
        public void StaticObjectIsUnaffectedByPreviewTime()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            ViewTransform view = new(1.0, 0, 0);
            Size renderSize = new(800, 600);

            Assert.True(Kept(sprites, Obj(400, 300), view, renderSize, previewSeconds: 74));
            Assert.False(Kept(sprites, Obj(5000, 300), view, renderSize, previewSeconds: 74));
        }

        private static bool Kept(
            SpriteCache sprites, LevelObject obj, ViewTransform view, Size renderSize, double? previewSeconds = null)
        {
            // Resolves the offset through the same single source the draw loop uses, so the test exercises
            // the pairing the renderer relies on rather than a cull box computed some other way.
            MethodInfo offsetMethod = SceneRenderer.GetMethod(
                "DrawOffset", BindingFlags.Public | BindingFlags.Static)!;
            object drawOffset = offsetMethod.Invoke(null, [obj, previewSeconds])!;

            MethodInfo boundsMethod = SceneRenderer.GetMethod(
                "CullBounds", BindingFlags.Public | BindingFlags.Static)!;
            LevelBounds bounds = (LevelBounds)boundsMethod.Invoke(
                null, [sprites, obj, 0, 0, false, drawOffset])!;

            return IsWithinViewport(bounds, view, renderSize, 256);
        }

        private static LevelObject Obj(double x, double y)
        {
            return Obj("candy", x, y);
        }

        private static LevelObject Obj(string type, double x, double y)
        {
            XElement element = new(type);
            element.SetAttributeValue("x", x.ToString(CultureInfo.InvariantCulture));
            element.SetAttributeValue("y", y.ToString(CultureInfo.InvariantCulture));
            return new LevelObject(element);
        }

        private static LevelObject Mover(string type, double x, double y, string path, int moveSpeed)
        {
            LevelObject obj = Obj(type, x, y);
            obj.Element.SetAttributeValue("path", path);
            obj.Element.SetAttributeValue("moveSpeed", moveSpeed.ToString(CultureInfo.InvariantCulture));
            return obj;
        }
    }
}
