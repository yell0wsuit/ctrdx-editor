using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
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
    }
}
