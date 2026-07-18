using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the pure placement data used to render the mechanical hand claw.</summary>
    public class HandRenderTests
    {
        private static LevelObject Hand(params (double Angle, double Length)[] segments)
        {
            XElement element = new(
                "hand",
                new XAttribute("x", "100"),
                new XAttribute("y", "200"),
                new XAttribute("segmentsCount", segments.Length));
            for (int i = 0; i < segments.Length; i++)
            {
                element.SetAttributeValue($"segment{i + 1}Angle", segments[i].Angle);
                element.SetAttributeValue($"segment{i + 1}Length", segments[i].Length);
                element.SetAttributeValue($"segment{i + 1}Rotatable", "true");
            }
            return new LevelObject(element);
        }

        /// <summary>The claw inherits the final segment's absolute world angle.</summary>
        [Fact]
        public void HandRenderUsesTerminalSegmentAngleForClaw()
        {
            AtlasFrame frame = Frame();

            HandClawPlacement placement = HandClawLayout.Compute(frame, Hand((0, 50), (-90, 70)));

            Assert.Equal(-90, placement.AngleDegrees);
            Assert.Equal(150, placement.Pivot.X, 6);
            Assert.Equal(130, placement.Pivot.Y, 6);
        }

        /// <summary>The visible pixels retain their offset inside the untrimmed claw box.</summary>
        [Fact]
        public void HandRenderPreservesClawTrimOffset()
        {
            AtlasFrame frame = Frame();

            HandClawPlacement placement = HandClawLayout.Compute(frame, Hand((0, 50)));

            Assert.Equal(150 - (96.0 / 6.0) + (16.0 / 3.0), placement.Sprite.Dest.X, 6);
            Assert.Equal(200 - (96.0 / 6.0) + (7.0 / 3.0), placement.Sprite.Dest.Y, 6);
            Assert.Equal(60.0 / 3.0, placement.Sprite.Dest.W, 6);
            Assert.Equal(70.0 / 3.0, placement.Sprite.Dest.H, 6);
        }

        /// <summary>The palette composition crops to a short arm and the visible claw.</summary>
        [Fact]
        public void HandRenderThumbnailBoundsContainShortArmAndClaw()
        {
            HandThumbnailComposition composition = HandThumbnailLayout.Compute(BoneFrame(), Frame(), armLength: 24);

            Assert.Equal(24, composition.ArmLength);
            Assert.Equal(0, composition.Bounds.X, 6);
            Assert.Equal(-41.0 / 3.0, composition.Bounds.Y, 6);
            Assert.Equal(100.0 / 3.0, composition.Bounds.W, 6);
            Assert.Equal(70.0 / 3.0, composition.Bounds.H, 6);
        }

        /// <summary>Hands bypass the generic multi-layer atlas thumbnail compositor.</summary>
        [Fact]
        public void HandRenderUsesCompositedThumbnailRoute()
        {
            MethodInfo? method = typeof(SpriteCache).GetMethod(
                "UsesCompositedThumbnail", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);
            Assert.True((bool)method.Invoke(null, ["hand"])!);
        }

        /// <summary>Hovering a segment tints its origin joint button with the same faint blue as its bone.</summary>
        [Fact]
        public void HandSegmentHoverTintsBoneAndJointButton()
        {
            string source = File.ReadAllText(SourcePath(
                "CtrDxEditor.Shared",
                "Rendering",
                "LevelCanvas.Rendering.cs"));

            Assert.Contains(
                "_palette.HandSegmentHoverTint, _palette.HandSegmentHoverTint",
                source,
                StringComparison.Ordinal);
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

        private static AtlasFrame Frame()
        {
            return new AtlasFrame(
                "hand_idle.png",
                new IntRect(0, 0, 60, 70),
                new IntRect(16, 7, 60, 70),
                new IntSize(96, 96),
                Rotated: false,
                Trimmed: true);
        }

        private static AtlasFrame BoneFrame()
        {
            return new AtlasFrame(
                "bone.png",
                new IntRect(0, 0, 12, 6),
                new IntRect(0, 0, 12, 6),
                new IntSize(12, 6),
                Rotated: false,
                Trimmed: false);
        }
    }
}
