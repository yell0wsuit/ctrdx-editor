using System;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

using Avalonia;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
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

        private static bool Kept(SpriteCache sprites, LevelObject obj, ViewTransform view, Size renderSize)
        {
            MethodInfo boundsMethod = SceneRenderer.GetMethod(
                "SelectionBounds", BindingFlags.Public | BindingFlags.Static)!;
            LevelBounds bounds = (LevelBounds)boundsMethod.Invoke(null, [sprites, obj, 0, 0, false])!;

            return IsWithinViewport(bounds, view, renderSize, 256);
        }

        private static LevelObject Obj(double x, double y)
        {
            XElement element = new("candy");
            element.SetAttributeValue("x", x.ToString(CultureInfo.InvariantCulture));
            element.SetAttributeValue("y", y.ToString(CultureInfo.InvariantCulture));
            return new LevelObject(element);
        }
    }
}
