using System;
using System.IO;

using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the canvas interaction mode gate, which headless pointer input cannot drive.</summary>
    public class CanvasInteractionModeTests
    {
        /// <summary>
        /// Edit is the enum's zero value, so a canvas that never had a mode assigned still edits.
        /// </summary>
        [Fact]
        public void EditIsTheDefaultMode()
        {
            Assert.Equal(CanvasInteractionMode.Edit, default);
        }

        /// <summary>The canvas property is initialized to edit rather than left at an implicit default.</summary>
        [Fact]
        public void CanvasInitializesToEditMode()
        {
            string canvas = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.cs"));

            Assert.Contains(
                "public CanvasInteractionMode InteractionMode { get; set; } = CanvasInteractionMode.Edit;",
                canvas,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// The pan gate runs before any hit-testing, so a press in pan mode can never reach an object.
        /// </summary>
        [Fact]
        public void PanGatePrecedesHitTesting()
        {
            string input = ReadInput();
            int gate = input.IndexOf("InteractionMode == CanvasInteractionMode.Pan", StringComparison.Ordinal);
            int hitTest = input.IndexOf("TopmostHit(", gate, StringComparison.Ordinal);

            Assert.True(gate >= 0, "The pan gate is missing from OnPointerPressed.");
            Assert.True(hitTest > gate, "Hit-testing must come after the pan gate, not before it.");
        }

        /// <summary>
        /// Pan mode leaves the selection alone, unlike the empty-space pan it sits in front of.
        /// </summary>
        /// <remarks>
        /// Framing a selected object is the main reason to reach for pan mode; clearing the selection on
        /// the way would defeat it.
        /// </remarks>
        [Fact]
        public void PanGateDoesNotClearSelection()
        {
            string input = ReadInput();
            int gate = input.IndexOf("InteractionMode == CanvasInteractionMode.Pan", StringComparison.Ordinal);
            int gateEnd = input.IndexOf("Point p = e.GetPosition(this);", gate, StringComparison.Ordinal);

            Assert.True(gate >= 0 && gateEnd > gate);
            Assert.DoesNotContain(
                "SelectionRequestKind.Clear",
                input.AsSpan(gate, gateEnd - gate),
                StringComparison.Ordinal);
        }

        /// <summary>A press consumed by the gate is marked handled so nothing downstream re-reads it.</summary>
        [Fact]
        public void PanGateHandlesThePress()
        {
            string input = ReadInput();
            int gate = input.IndexOf("InteractionMode == CanvasInteractionMode.Pan", StringComparison.Ordinal);
            int gateEnd = input.IndexOf("Point p = e.GetPosition(this);", gate, StringComparison.Ordinal);

            Assert.True(gate >= 0 && gateEnd > gate);
            Assert.Contains(
                "e.Handled = true;",
                input.AsSpan(gate, gateEnd - gate),
                StringComparison.Ordinal);
        }

        private static string ReadInput()
        {
            return File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));
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
