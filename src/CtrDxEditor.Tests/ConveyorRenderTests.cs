using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Smoke tests for conveyor rendering. The xUnit host has no Avalonia render backend, so these
    /// exercise the scene wiring (draw-order slot) and the geometry the renderer lays pieces over
    /// rather than a real <c>DrawingContext</c>, matching how <see cref="MouseRenderTests"/> tests
    /// scene classification by reflection.
    /// </summary>
    public class ConveyorRenderTests
    {
        private static LevelObject Belt(double length, double width, double angle, string? type = null)
        {
            XElement e = new("transporter");
            e.SetAttributeValue("x", "100");
            e.SetAttributeValue("y", "200");
            e.SetAttributeValue("length", length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            e.SetAttributeValue("width", width.ToString(System.Globalization.CultureInfo.InvariantCulture));
            e.SetAttributeValue("angle", angle.ToString(System.Globalization.CultureInfo.InvariantCulture));
            e.SetAttributeValue("velocity", "10");
            e.SetAttributeValue("direction", "forward");
            if (type is not null)
            {
                e.SetAttributeValue("type", type);
            }
            return new LevelObject(e);
        }

        private static int GameDrawLayer(LevelObject obj)
        {
            System.Type renderer = typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;
            MethodInfo method = renderer.GetMethod("GameDrawLayer", BindingFlags.Public | BindingFlags.Static)!;
            return (int)method.Invoke(null, [obj])!;
        }

        /// <summary>Conveyors share the vinyl draw tier and sit strictly behind bubbles.</summary>
        [Fact]
        public void ConveyorDrawsBehindBubblesOnTheVinylTier()
        {
            // The game draws conveyors right after the vinyl discs and before bubbles, so the belt sits
            // on the vinyl tier and strictly behind bubbles/pumps/spikes/candy.
            int belt = GameDrawLayer(Belt(250, 50, 0));
            int vinyl = GameDrawLayer(new LevelObject(new XElement("rotatedCircle")));
            int bubble = GameDrawLayer(new LevelObject(new XElement("bubble")));
            Assert.Equal(vinyl, belt);
            Assert.True(belt < bubble);
        }

        /// <summary>A wider belt produces taller bounds than a narrow one.</summary>
        [Fact]
        public void WiderBeltMapsToAWiderScreenSpan()
        {
            // The renderer lays pieces over ConveyorGeometry's bounds; a wider belt must span more.
            LevelBounds narrow = ConveyorGeometry.Bounds(ConveyorGeometry.Of(Belt(250, 50, 0))!.Value);
            LevelBounds wide = ConveyorGeometry.Bounds(ConveyorGeometry.Of(Belt(250, 120, 0))!.Value);
            Assert.True(wide.H > narrow.H);
        }
    }
}
