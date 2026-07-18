using System;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the shared mover/ant editable-path adapter used by canvas interactions.</summary>
    public class AntCanvasEditingTests
    {
        private static Type EditablePath =>
            typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.EditablePath")!;

        /// <summary>A closed ant exposes unique handles and moving one retains the terminal anchor marker.</summary>
        [Fact]
        public void ClosedAntPathExposesUniqueEditablePoints()
        {
            LevelObject ants = Obj("100,0,100,100,0,0");
            object path = For(ants)!;

            Assert.Equal(3, Points(path).Length);
            Invoke(path, "MovePoint", 2, new Vec2(90, 120));

            Assert.EndsWith(",0,0", ants.GetAttr("path"));
            Assert.Equal("100,0,90,120,0,0", ants.GetAttr("path"));
        }

        /// <summary>Closed ants expose their last-to-anchor segment while open ants do not invent one.</summary>
        [Fact]
        public void SegmentCountReflectsExplicitAntClosure()
        {
            object open = For(Obj("100,0,100,100"))!;
            object closed = For(Obj("100,0,100,100,0,0"))!;

            Assert.Equal(2, Property<int>(open, "SegmentCount"));
            Assert.Equal(3, Property<int>(closed, "SegmentCount"));
        }

        /// <summary>Insertion, append, and deletion all route through ant semantics and preserve closure.</summary>
        [Fact]
        public void AntMutationsPreserveClosureAndDeletionMinimum()
        {
            LevelObject ants = Obj("100,0,0,0");
            object path = For(ants)!;

            Invoke(path, "InsertPoint", 1, new Vec2(50, 50));
            Invoke(path, "AppendPoint", new Vec2(0, 100));
            Invoke(path, "DeletePoint", 1);

            Assert.Equal("50,50,0,100,0,0", ants.GetAttr("path"));

            Invoke(path, "DeletePoint", 1);
            Assert.Equal("0,100,0,0", ants.GetAttr("path"));
            Invoke(path, "DeletePoint", 1);
            Assert.Equal("0,100,0,0", ants.GetAttr("path"));
        }

        /// <summary>The adapter keeps existing mover retrace serialization behavior.</summary>
        [Fact]
        public void MoverAdapterPreservesRetraceSemantics()
        {
            LevelObject mover = new(new XElement(
                "star",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("path", "100,0,100,50,0,50,100,50,100,0"),
                new XAttribute("moveSpeed", "50")));
            object path = For(mover)!;

            Invoke(path, "MovePoint", 2, new Vec2(120, 50));

            Assert.Equal("100,0,120,50,0,50,120,50,100,0", mover.GetAttr("path"));
        }

        /// <summary>Ant objects are directly node-editable even though they are not mover paths.</summary>
        [Fact]
        public void CanvasRecognizesAntsAsEditablePolylines()
        {
            MethodInfo method = typeof(LevelCanvas).GetMethod(
                "IsEditablePolyline", BindingFlags.Static | BindingFlags.NonPublic)!;

            Assert.True((bool)method.Invoke(null, [Obj("100,0")])!);
        }

        /// <summary>Ant selection follows path segments instead of selecting empty space inside the bounds.</summary>
        [Fact]
        public void AntSelectionHitTestTracksVisibleSegments()
        {
            MethodInfo method = typeof(SpriteCache).Assembly
                .GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!
                .GetMethod("SelectionContains", BindingFlags.Public | BindingFlags.Static)!;
            LevelObject ants = Obj("100,0,100,100");
            LevelBounds bounds = new(-16, -16, 132, 132);

            Assert.True((bool)method.Invoke(null, [ants, bounds, new Vec2(100, 50), 0d, null])!);
            Assert.False((bool)method.Invoke(null, [ants, bounds, new Vec2(40, 50), 0d, null])!);
        }

        private static object? For(LevelObject obj)
        {
            return EditablePath.GetMethod("For", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [obj]);
        }

        private static Vec2[] Points(object path)
        {
            return Property<Vec2[]>(path, "Points");
        }

        private static T Property<T>(object path, string name)
        {
            return (T)path.GetType().GetProperty(name)!.GetValue(path)!;
        }

        private static void Invoke(object path, string method, params object[] args)
        {
            _ = path.GetType().GetMethod(method)!.Invoke(path, args);
        }

        private static LevelObject Obj(string path)
        {
            return new LevelObject(new XElement(
                "ants",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("path", path),
                new XAttribute("moveSpeed", "100")));
        }
    }
}
