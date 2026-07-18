using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests canvas interaction gating for effectively hidden objects.</summary>
    [Collection(LevelCanvasTestGroup.Name)]
    public class LevelCanvasVisibilityTests
    {
        /// <summary>Verifies that a hidden top object is skipped so the visible object underneath is picked.</summary>
        [Fact]
        public void HiddenObjectIsNotPicked()
        {
            LevelObject visible = new(new XElement("candy", new XAttribute("x", "50"), new XAttribute("y", "50")));
            LevelObject hidden = new(new XElement("star", new XAttribute("x", "50"), new XAttribute("y", "50")));
            IReadOnlyList<LevelObject> objects = [visible, hidden];
            List<LevelBounds> bounds =
            [
                new LevelBounds(40, 40, 20, 20),
                new LevelBounds(40, 40, 20, 20),
            ];
            LevelCanvas canvas = new()
            {
                HiddenObjects = new HashSet<LevelObject> { hidden },
            };
            MethodInfo? method = typeof(LevelCanvas).GetMethod(
                "TopmostHit",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            int hit = (int)method.Invoke(canvas, [objects, bounds, new Vec2(50, 50), -1])!;

            Assert.Equal(0, hit);
        }

        /// <summary>Verifies that the shared bounds test rejects hidden objects in lock and hover paths.</summary>
        [Fact]
        public void HiddenObjectFailsSharedBoundsHitTest()
        {
            LevelObject hidden = new(new XElement("star", new XAttribute("x", "50"), new XAttribute("y", "50")));
            LevelCanvas canvas = new()
            {
                HiddenObjects = new HashSet<LevelObject> { hidden },
            };
            MethodInfo? method = typeof(LevelCanvas).GetMethod(
                "HitBoundContains",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            bool hit = (bool)method.Invoke(
                canvas,
                [hidden, new LevelBounds(40, 40, 20, 20), new Vec2(50, 50)])!;

            Assert.False(hit);
        }
    }
}
