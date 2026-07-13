using System.Reflection;
using System.Linq;
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

        /// <summary>The static editor preview contains every visual node built by the game.</summary>
        [Fact]
        public void ConveyorLayoutPortsEveryGameVisualPiece()
        {
            ConveyorVisualLayout layout = ConveyorVisualLayout.Build(250, 50, arrowSign: 1);

            Assert.Equal(13, layout.Pieces.Count);
            Assert.Equal(1, layout.Pieces.Count(p => p.Kind == ConveyorVisualPieceKind.Middle));
            Assert.Equal(2, layout.Pieces.Count(p => p.Kind == ConveyorVisualPieceKind.End));
            Assert.Equal(2, layout.Pieces.Count(p => p.Kind == ConveyorVisualPieceKind.Side));
            Assert.Equal(4, layout.Pieces.Count(p => p.Kind == ConveyorVisualPieceKind.Corner));
            Assert.Equal(1, layout.Pieces.Count(p => p.Kind == ConveyorVisualPieceKind.PlateSurface));
            Assert.Equal(1, layout.Pieces.Count(p => p.Kind == ConveyorVisualPieceKind.Arrow));
            Assert.Equal(2, layout.Pieces.Count(p => p.Kind == ConveyorVisualPieceKind.Highlight));
        }

        /// <summary>The port preserves the game's authored scaling and cap inset constants.</summary>
        [Fact]
        public void ConveyorLayoutUsesGameScaleAndOffsetConstants()
        {
            ConveyorVisualLayout layout = ConveyorVisualLayout.Build(250, 50, arrowSign: 1);
            ConveyorVisualPiece middle = Assert.Single(layout.Pieces, p => p.Kind == ConveyorVisualPieceKind.Middle);
            ConveyorVisualPiece plate = Assert.Single(layout.Pieces, p => p.Kind == ConveyorVisualPieceKind.PlateSurface);
            ConveyorVisualPiece[] ends = [.. layout.Pieces.Where(p => p.Kind == ConveyorVisualPieceKind.End)];

            Assert.Equal(250, middle.Bounds.H, 6);
            Assert.Equal(50 - (10.0 / SpritePlacement.MapScale), middle.Bounds.W, 6);
            Assert.Equal(40, plate.Bounds.W, 6);
            Assert.Equal(250, plate.Bounds.H, 6);
            Assert.Contains(ends, p => p.OffsetY == 6);
            Assert.Contains(ends, p => p.OffsetY == -6);
            Assert.All(ends, p => Assert.Equal(30, p.Bounds.W, 6));
        }

        /// <summary>Fractional XML dimensions retain the game's ceiling and integer-pivot artifacts.</summary>
        [Fact]
        public void ConveyorLayoutMatchesGameRoundingForFractionalDimensions()
        {
            ConveyorVisualLayout layout = ConveyorVisualLayout.Build(250.2, 50.2, arrowSign: 1);

            Assert.Equal(151.0 / SpritePlacement.MapScale, layout.RootWidth, 6);
            Assert.Equal(751.0 / SpritePlacement.MapScale, layout.RootHeight, 6);
            Assert.Equal(750.0 / SpritePlacement.MapScale, layout.RootTranslationX, 6);
            Assert.Equal(-75.0 / SpritePlacement.MapScale, layout.RootTranslationY, 6);
            Assert.Equal(-0.1, layout.ParentRotationPivotX, 6);
            ConveyorVisualPiece plate = Assert.Single(layout.Pieces,
                p => p.Kind == ConveyorVisualPieceKind.PlateSurface);
            Assert.Equal((75 - (117 * (120.8 / 235))) / SpritePlacement.MapScale, plate.Bounds.X, 6);
            Assert.Equal(120.8 / SpritePlacement.MapScale, plate.Bounds.W, 6);
        }

        /// <summary>Representative nodes preserve the game's integer anchor and scale pivots.</summary>
        [Fact]
        public void ConveyorLayoutMatchesGamePiecePositions()
        {
            ConveyorVisualLayout layout = ConveyorVisualLayout.Build(250, 50, arrowSign: 1);
            ConveyorVisualPiece middle = Assert.Single(layout.Pieces, p => p.Kind == ConveyorVisualPieceKind.Middle);
            ConveyorVisualPiece bottomEnd = Assert.Single(layout.Pieces,
                p => p.Kind == ConveyorVisualPieceKind.End && p.OffsetY > 0);
            ConveyorVisualPiece bottomHighlight = Assert.Single(layout.Pieces,
                p => p.Kind == ConveyorVisualPieceKind.Highlight && p.Bounds.Y > 0);

            Assert.Equal(5.0 / 3.0, middle.Bounds.X, 6);
            Assert.Equal(125.0 / 83.0, middle.Bounds.Y, 6);
            Assert.Equal(10, bottomEnd.Bounds.X, 6);
            Assert.Equal(689.0 / 3.0, bottomEnd.Bounds.Y, 6);
            Assert.Equal(26.0 + (2.0 / 6.0), bottomEnd.Bounds.H, 6);
            Assert.Equal(5.08510638297872, bottomHighlight.Bounds.X, 6);
            Assert.Equal(242, bottomHighlight.Bounds.Y, 6);
        }

        /// <summary>Mirrors and arrow omission match ConveyorBelt.BuildVisuals.</summary>
        [Fact]
        public void ConveyorLayoutPreservesMirrorsAndManualArrowBehavior()
        {
            ConveyorVisualLayout automatic = ConveyorVisualLayout.Build(250, 50, arrowSign: -1);
            ConveyorVisualLayout manual = ConveyorVisualLayout.Build(250, 50, arrowSign: 0);

            Assert.Equal(2, automatic.Pieces.Count(p => p.Kind == ConveyorVisualPieceKind.Side));
            _ = Assert.Single(automatic.Pieces, p => p.Kind == ConveyorVisualPieceKind.Side && p.FlipX);
            Assert.Equal(3, automatic.Pieces.Count(p => p.Kind == ConveyorVisualPieceKind.Corner && (p.FlipX || p.FlipY)));
            Assert.Equal(-1, Assert.Single(automatic.Pieces, p => p.Kind == ConveyorVisualPieceKind.Arrow).Direction);
            Assert.DoesNotContain(manual.Pieces, p => p.Kind == ConveyorVisualPieceKind.Arrow);
        }

        /// <summary>The palette crop includes the frame overhang at both transporter ends.</summary>
        [Fact]
        public void ConveyorThumbnailBoundsIncludeCompleteComposedFrame()
        {
            LevelBounds bounds = ConveyorVisualLayout.Build(100, 50, arrowSign: 1).BeltLocalBounds();

            Assert.Equal(-6, bounds.X, 6);
            Assert.Equal(112.0 + (1.0 / 3.0), bounds.W, 6);
            Assert.Equal(-25.0 - (1.0 / 3.0), bounds.Y, 6);
            Assert.Equal(25, bounds.Y + bounds.H, 6);
        }

        /// <summary>Transporters bypass the generic centered-layer thumbnail compositor.</summary>
        [Fact]
        public void ConveyorUsesCompositedPaletteThumbnailRoute()
        {
            MethodInfo? method = typeof(SpriteCache).GetMethod(
                "UsesCompositedThumbnail", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);
            Assert.True((bool)method.Invoke(null, ["transporter"])!);
            Assert.False((bool)method.Invoke(null, ["bubble"])!);
        }
    }
}
