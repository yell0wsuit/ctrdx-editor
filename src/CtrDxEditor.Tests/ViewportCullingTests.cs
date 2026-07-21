using System;
using System.Reflection;

using Avalonia;

using CtrDxEditor.Content;
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
    }
}
