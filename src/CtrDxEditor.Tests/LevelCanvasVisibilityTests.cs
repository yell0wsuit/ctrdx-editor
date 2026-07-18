using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
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

        /// <summary>A visible rope keeps its authored bulb target even when that bulb is hidden.</summary>
        [Fact]
        public void VisibleRopeStillResolvesHiddenBulbTarget()
        {
            LevelDocument doc = LevelDocument.Parse("""
                <map>
                    <layer name="settings">
                        <map width="320" height="480" />
                        <gameDesign ropePhysicsSpeed="1" />
                    </layer>
                    <layer name="objects">
                        <candy x="40" y="50" candyNumber="0" />
                        <lightBulb x="220" y="230" number="0" />
                        <grab x="100" y="110" radius="-1" bindBulb="true" bulbNumber="0" />
                    </layer>
                </map>
                """);
            LevelObject candy = Assert.Single(doc.AllObjects, obj => obj.Type == "candy");
            LevelObject bulb = Assert.Single(doc.AllObjects, obj => obj.Type == "lightBulb");
            LevelObject grab = Assert.Single(doc.AllObjects, obj => obj.Type == "grab");
            LevelCanvas canvas = new()
            {
                HiddenObjects = new HashSet<LevelObject> { bulb },
            };
            MethodInfo? method = typeof(LevelCanvas).GetMethod(
                "BuildRopeForVisibleGrab",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            RopeVisual rope = (RopeVisual)method.Invoke(canvas, [grab, doc])!;

            Assert.NotEqual(new Vec2(candy.X, candy.Y), rope.SamplePoints[^1]);
            Assert.Equal(new Vec2(bulb.X, bulb.Y), rope.SamplePoints[^1]);
        }
    }
}
