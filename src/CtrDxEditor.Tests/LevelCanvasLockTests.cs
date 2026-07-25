using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests canvas interaction gating for objects in a locked layer.</summary>
    [Collection(LevelCanvasTestGroup.Name)]
    public class LevelCanvasLockTests
    {
        /// <summary>A locked-out top object is skipped so the interactive object underneath is picked.</summary>
        [Fact]
        public void LockedOutObjectIsNotPicked()
        {
            LevelObject open = new(new XElement("candy", new XAttribute("x", "50"), new XAttribute("y", "50")));
            LevelObject locked = new(new XElement("star", new XAttribute("x", "50"), new XAttribute("y", "50")));
            IReadOnlyList<LevelObject> objects = [open, locked];
            List<LevelBounds> bounds =
            [
                new LevelBounds(40, 40, 20, 20),
                new LevelBounds(40, 40, 20, 20),
            ];
            LevelCanvas canvas = new()
            {
                LockedOutObjects = new HashSet<LevelObject> { locked },
            };
            MethodInfo? method = typeof(LevelCanvas).GetMethod(
                "TopmostHit",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            int hit = (int)method.Invoke(canvas, [objects, bounds, new Vec2(50, 50), -1])!;

            Assert.Equal(0, hit);
        }

        /// <summary>The shared bounds test rejects a locked-out object in lock and hover paths.</summary>
        [Fact]
        public void LockedOutObjectFailsSharedBoundsHitTest()
        {
            LevelObject locked = new(new XElement("star", new XAttribute("x", "50"), new XAttribute("y", "50")));
            LevelCanvas canvas = new()
            {
                LockedOutObjects = new HashSet<LevelObject> { locked },
            };
            MethodInfo? method = typeof(LevelCanvas).GetMethod(
                "HitBoundContains",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            bool hit = (bool)method.Invoke(
                canvas,
                [locked, new LevelBounds(40, 40, 20, 20), new Vec2(50, 50)])!;

            Assert.False(hit);
        }

        /// <summary>Publishing a locked-out set clears selected and pinned objects before direct editing.</summary>
        [Fact]
        public void LockedOutSetClearsSelectionAndPin()
        {
            LevelObject locked = new(new XElement("star", new XAttribute("x", "50"), new XAttribute("y", "50")));
            LevelCanvas canvas = new()
            {
                SelectedObject = locked,
                LockedObject = locked,
                LockedOutObjects = new HashSet<LevelObject> { locked },
            };

            Assert.Null(canvas.SelectedObject);
            Assert.Null(canvas.LockedObject);
        }

        /// <summary>A pinned object captures its drag origin so pointer movement can update its position.</summary>
        [Fact]
        public void PinnedObjectDragCapturesOrigin()
        {
            string input = File.ReadAllText(SourcePath(
                "CtrDxEditor.Shared",
                "Rendering",
                "LevelCanvas.Input.cs"));
            int lockedBranch = input.IndexOf(
                "if (LockedObject is { } locked)",
                StringComparison.Ordinal);
            int normalBranch = input.IndexOf(
                "int after = _lastHitIndex",
                lockedBranch,
                StringComparison.Ordinal);

            Assert.True(lockedBranch >= 0);
            Assert.True(normalBranch > lockedBranch);
            Assert.Contains(
                "CaptureGroupDragOrigins(locked);",
                input[lockedBranch..normalBranch],
                StringComparison.Ordinal);
        }

        /// <summary>Specialized rail handles are disabled while more than one object is selected.</summary>
        [Fact]
        public void RailHandlesAreSuppressedWhenMultipleObjectsSelected()
        {
            LevelObject grab = new(XElement.Parse(
                "<grab x=\"100\" y=\"100\" moveLength=\"100\" moveOffset=\"0\" />"));
            LevelObject other = new(XElement.Parse("<star x=\"200\" y=\"200\" />"));
            LevelCanvas canvas = new()
            {
                SelectedObject = grab,
                SelectedObjects = new HashSet<LevelObject> { grab, other },
            };
            MethodInfo? method = typeof(LevelCanvas).GetMethod(
                "HitRail",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            GrabRail.Handle hit = (GrabRail.Handle)method.Invoke(canvas, [new Vec2(100, 100)])!;

            Assert.Equal(GrabRail.Handle.None, hit);
        }

        private static string SourcePath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine([path, .. parts]);
        }
    }
}
