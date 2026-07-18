using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests ant renderer scene integration and its pure layout bridge.</summary>
    public class AntRenderTests
    {
        private static readonly Type SceneRenderer =
            typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;

        private static Type AntRenderer =>
            typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.AntRenderer")!;

        /// <summary>Ant selection covers all vertices instead of falling back to the object anchor.</summary>
        [Fact]
        public void AntPathBoundsCoverEveryVertex()
        {
            LevelObject ants = Obj(20, 30, "100,0,100,80");
            MethodInfo method = SceneRenderer.GetMethod(
                "SelectionBounds", BindingFlags.Public | BindingFlags.Static)!;

            LevelBounds bounds = (LevelBounds)method.Invoke(
                null, [new SpriteCache(new EmptyContentStore()), ants, 0, 0, false])!;

            Assert.True(bounds.Contains(new Vec2(120, 110)));
            Assert.Equal(new LevelBounds(-16, -6, 172, 152), bounds);
        }

        /// <summary>Ant art draws after conveyors but before candy in the editor's fixed game order.</summary>
        [Fact]
        public void AntsDrawAfterConveyorsBeforeCandy()
        {
            MethodInfo method = SceneRenderer.GetMethod(
                "GameDrawLayer", BindingFlags.Public | BindingFlags.Static)!;

            int conveyor = (int)method.Invoke(null, [Obj("transporter")])!;
            int ants = (int)method.Invoke(null, [Obj("ants")])!;
            int candy = (int)method.Invoke(null, [Obj("candy")])!;

            Assert.True(conveyor <= ants);
            Assert.True(ants < candy);
        }

        /// <summary>The renderer bridge supplies deterministic static frames and open-path holes.</summary>
        [Fact]
        public void StaticRendererLayoutIsDeterministicAndShowsOpenHoles()
        {
            MethodInfo method = AntRenderer.GetMethod(
                "BuildLayout", BindingFlags.Public | BindingFlags.Static)!;
            LevelObject ants = Obj(0, 0, "105,0");

            AntVisualLayout first = (AntVisualLayout)method.Invoke(null, [ants, null])!;
            AntVisualLayout second = (AntVisualLayout)method.Invoke(null, [ants, null])!;

            Assert.Equal(first.Ants, second.Ants);
            Assert.Equal(2, first.Holes.Count);
            Assert.Equal([0, 1, 2], first.Ants.Select(a => a.Frame));
        }

        /// <summary>Live elapsed time and negative authored speed flow into the renderer's layout.</summary>
        [Fact]
        public void RendererLayoutSupportsLiveReverseMovement()
        {
            MethodInfo method = AntRenderer.GetMethod(
                "BuildLayout", BindingFlags.Public | BindingFlags.Static)!;
            LevelObject ants = Obj(0, 0, "105,0", moveSpeed: "-10");

            AntVisualLayout layout = (AntVisualLayout)method.Invoke(null, [ants, 1d])!;

            Assert.Equal(-25.5, layout.Ants[0].PathOffset, 6);
            Assert.Equal(new Vec2(-25.5, 0), layout.Ants[0].Position);
        }

        /// <summary>Explicitly closed renderer layouts omit both endpoint-hole sprites.</summary>
        [Fact]
        public void ClosedRendererLayoutOmitsHoles()
        {
            MethodInfo method = AntRenderer.GetMethod(
                "BuildLayout", BindingFlags.Public | BindingFlags.Static)!;

            AntVisualLayout layout = (AntVisualLayout)method.Invoke(
                null, [Obj(0, 0, "100,0,0,0"), null])!;

            Assert.True(layout.Closed);
            Assert.Empty(layout.Holes);
        }

        /// <summary>Ant art uses the game's trimmed quad size and integer center anchor.</summary>
        [Fact]
        public void TrimmedPlacementMatchesGameImageAnchor()
        {
            MethodInfo method = AntRenderer.GetMethod(
                "ComputeTrimmedPlacement", BindingFlags.Public | BindingFlags.Static)!;
            AtlasFrame hole = new(
                "ant_hole.png",
                new IntRect(1, 451, 93, 101),
                new IntRect(0, 0, 93, 101),
                new IntSize(98, 126),
                Rotated: false,
                Trimmed: true);

            SpriteLayout placement = (SpriteLayout)method.Invoke(null, [hole, new Vec2(100, 200), 1d])!;

            Assert.Equal(100 - (46d / 3), placement.Dest.X, 6);
            Assert.Equal(200 - (50d / 3), placement.Dest.Y, 6);
            Assert.Equal(31, placement.Dest.W, 6);
            Assert.Equal(101d / 3, placement.Dest.H, 6);
        }

        private static LevelObject Obj(int x, int y, string path, string moveSpeed = "100")
        {
            return new LevelObject(new XElement(
                "ants",
                new XAttribute("x", x),
                new XAttribute("y", y),
                new XAttribute("path", path),
                new XAttribute("moveSpeed", moveSpeed)));
        }

        private static LevelObject Obj(string type)
        {
            return new LevelObject(new XElement(type));
        }
    }
}
