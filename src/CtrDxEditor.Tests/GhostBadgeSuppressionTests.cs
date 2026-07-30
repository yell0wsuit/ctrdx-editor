using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// The ghost morph selector and the drag readout both anchor above the selection outline, so exactly
    /// one of them may draw at a time. These guard that gate, which headless pointer input cannot drive.
    /// </summary>
    public class GhostBadgeSuppressionTests
    {
        /// <summary>The selector lives in its own partial rather than bloating the rendering partial.</summary>
        [Fact]
        public void GhostBadgeLivesInItsOwnPartial()
        {
            Assert.True(File.Exists(SourcePath(
                "CtrDxEditor.Shared", "Rendering", "LevelCanvas.GhostBadge.cs")));
            Assert.DoesNotContain(
                "private void DrawGhostBadge",
                File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Rendering.cs")),
                StringComparison.Ordinal);
        }

        /// <summary>The selector returns early while a drag is active, leaving the slot to the readout.</summary>
        [Fact]
        public void GhostBadgeYieldsToADrag()
        {
            string source = File.ReadAllText(SourcePath(
                "CtrDxEditor.Shared", "Rendering", "LevelCanvas.GhostBadge.cs"));

            int method = source.IndexOf("private void DrawGhostBadge", StringComparison.Ordinal);
            int gate = source.IndexOf("AnyDragActive", method, StringComparison.Ordinal);
            int firstDraw = source.IndexOf("context.", method, StringComparison.Ordinal);

            Assert.True(method >= 0, "DrawGhostBadge is missing.");
            Assert.True(gate > method, "DrawGhostBadge must consult AnyDragActive.");
            Assert.True(gate < firstDraw, "The drag gate must precede any drawing.");
        }

        /// <summary>A suppressed selector registers no hit rects, so a mid-drag click cannot land on it.</summary>
        [Fact]
        public void SuppressedSelectorRegistersNoHitTargets()
        {
            string source = File.ReadAllText(SourcePath(
                "CtrDxEditor.Shared", "Rendering", "LevelCanvas.GhostBadge.cs"));

            int gate = source.IndexOf("AnyDragActive", StringComparison.Ordinal);
            int hits = source.IndexOf("_ghostIconHits.Add", gate, StringComparison.Ordinal);

            Assert.True(hits > gate, "Hit targets must only be recorded after the drag gate passes.");
        }

        /// <summary>Every drag flag feeds the gate, so no drag type slips past it.</summary>
        [Theory]
        [InlineData("_dragging")]
        [InlineData("_rotating")]
        [InlineData("_resizingRadius")]
        [InlineData("_ropeDrag")]
        [InlineData("_railDrag")]
        [InlineData("_stripResizeDrag")]
        [InlineData("_conveyorDrag")]
        [InlineData("_vinylHandleDrag")]
        [InlineData("_polylinePointDrag")]
        [InlineData("_handJointDrag")]
        [InlineData("_handBaseDrag")]
        [InlineData("_resizingTutorialText")]
        [InlineData("_waterDrag")]
        public void EveryDragFlagFeedsTheGate(string flag)
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.cs"));

            int property = source.IndexOf("private bool AnyDragActive", StringComparison.Ordinal);
            Assert.True(property >= 0, "AnyDragActive is missing from LevelCanvas.");

            int end = source.IndexOf('}', source.IndexOf("=>", property, StringComparison.Ordinal));
            string body = source[property..(end < 0 ? source.Length : end)];

            Assert.Contains(flag, body, StringComparison.Ordinal);
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
