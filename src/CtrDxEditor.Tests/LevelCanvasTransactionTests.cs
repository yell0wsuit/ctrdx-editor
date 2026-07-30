using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests canvas transaction wiring that is difficult to exercise through headless pointer capture.</summary>
    public class LevelCanvasTransactionTests
    {
        /// <summary>Verifies lost pointer capture ends the same document-edit gesture as pointer release.</summary>
        [Fact]
        public void PointerCaptureLostCompletesDocumentEditGesture()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));

            Assert.Contains("protected override void OnPointerCaptureLost", source, StringComparison.Ordinal);
            Assert.Contains("EndPointerGesture();", source, StringComparison.Ordinal);
            Assert.Contains("CompleteDocumentEdit?.Invoke();", source, StringComparison.Ordinal);
        }

        /// <summary>Rope hover chrome clears when its gesture, pointer presence, or selected rope ends.</summary>
        [Fact]
        public void RopeHoverClearsAcrossCanvasLifecycleChanges()
        {
            string input = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));
            string canvas = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.cs"));

            int gesture = input.IndexOf("private void EndPointerGesture()", StringComparison.Ordinal);
            int pointerExit = input.IndexOf("protected override void OnPointerExited", gesture, StringComparison.Ordinal);
            int keyDown = input.IndexOf("protected override void OnKeyDown", pointerExit, StringComparison.Ordinal);
            int selection = canvas.IndexOf("private void HandleSelectionChanged()", StringComparison.Ordinal);
            int resetPolyline = canvas.IndexOf("private void ResetPolylineHover()", selection, StringComparison.Ordinal);

            Assert.True(gesture >= 0 && pointerExit > gesture && keyDown > pointerExit);
            Assert.True(selection >= 0 && resetPolyline > selection);
            Assert.Contains("SetRopeHovered(false);", input.AsSpan(gesture, pointerExit - gesture), StringComparison.Ordinal);
            Assert.Contains("SetRopeHovered(false);", input.AsSpan(pointerExit, keyDown - pointerExit), StringComparison.Ordinal);
            Assert.Contains("SetRopeHovered(false);", canvas.AsSpan(selection, resetPolyline - selection), StringComparison.Ordinal);
        }

        /// <summary>Hand body and button presses remain selection-only until deliberate pointer travel.</summary>
        [Fact]
        public void HandDragsWaitForThresholdBeforeEditing()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));

            Assert.Contains("HandGeometry.HasDragged(_handDragStartPointer, levelPt, View.Zoom)", source, StringComparison.Ordinal);
            Assert.Contains("if (!BeginHandDrag(levelPt))", source, StringComparison.Ordinal);
            Assert.Contains("bool draggingSingleHand = IsSingleSelection && HandObject.IsHand(obj.Type);", source, StringComparison.Ordinal);
            Assert.Contains("_handObjectDrag = draggingSingleHand;", source, StringComparison.Ordinal);
            Assert.Contains("(_handJointDrag > 0 || _handBaseDrag) && _handDragHasMoved", source, StringComparison.Ordinal);
        }

        /// <summary>Hand buttons activate their owning segment, while a real joint drag follows its edited segment.</summary>
        [Fact]
        public void HandButtonsSelectTheirStartingSegment()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));

            Assert.Contains(
                "ActivateHandSegment(HandGeometry.ButtonSegment(pressedHandHit, HandObject.SegmentCount(handObj)));",
                source,
                StringComparison.Ordinal);
            Assert.Contains("ActivateHandSegment(_handJointDrag);", source, StringComparison.Ordinal);
        }

        /// <summary>Hovering a hand button previews the same owning segment that clicking it activates.</summary>
        [Fact]
        public void HandButtonsHoverTheirStartingSegment()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));

            Assert.Contains(
                "HandGeometry.ButtonSegment(hit, HandObject.SegmentCount(hand))",
                source,
                StringComparison.Ordinal);
        }

        /// <summary>Movement path visibility is exposed through the View menu and bound into the canvas.</summary>
        [Fact]
        public void ViewMenuWiresMovementPathToggleToCanvas()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string codeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Commands.cs"));

            Assert.Contains("ShowMovementPathsToggle_Click", view, StringComparison.Ordinal);
            Assert.Contains("Menu.View.ShowMovementPaths", view, StringComparison.Ordinal);
            Assert.Contains("ShowMovementPaths=\"{Binding ShowMovementPaths}\"", view, StringComparison.Ordinal);
            Assert.Contains("vm.ShowMovementPaths = !vm.ShowMovementPaths;", codeBehind, StringComparison.Ordinal);
        }

        /// <summary>Retrace is exposed as a polyline property field driven by SetRetrace.</summary>
        [Fact]
        public void RetraceIsExposedAsPolylineField()
        {
            string builder = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "ViewModels", "SpinFieldBuilder.cs"));

            Assert.Contains("polylineRetrace", builder, StringComparison.Ordinal);
            Assert.Contains("MoverPath.SetRetrace", builder, StringComparison.Ordinal);
            Assert.Contains("MoverPath.IsRetrace", builder, StringComparison.Ordinal);
            Assert.DoesNotContain("polylineLoop", builder, StringComparison.Ordinal);
        }

        /// <summary>The polyline editor has no mode toggle anywhere in the view or view model.</summary>
        [Fact]
        public void PolylineEditingHasNoModeToggle()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string vm = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "ViewModels", "EditorViewModel.cs"));
            string canvas = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.cs"));

            Assert.DoesNotContain("PolylineEditMode", view, StringComparison.Ordinal);
            Assert.DoesNotContain("PolylineEditMode", vm, StringComparison.Ordinal);
            Assert.DoesNotContain("Menu.Edit.PolylineEditPoints", view, StringComparison.Ordinal);
            Assert.Contains("CanEditPolyline", vm, StringComparison.Ordinal);
            Assert.Contains("change.Property == SelectedObjectProperty", canvas, StringComparison.Ordinal);
            Assert.Contains("ResetPolylineHover();", canvas, StringComparison.Ordinal);
        }

        /// <summary>Polyline gestures append via the nub and delete single vertices; the old mode/truncate paths are gone.</summary>
        [Fact]
        public void PolylineGesturesUseNubAppendAndSingleVertexDelete()
        {
            string input = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));
            string rendering = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Rendering.cs"));
            string adapter = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "EditablePath.cs"));

            Assert.Contains("HitPolylineNub", input, StringComparison.Ordinal);
            Assert.Contains("EditablePath.For", input, StringComparison.Ordinal);
            Assert.Contains(".AppendPoint(", input, StringComparison.Ordinal);
            Assert.Contains(".DeletePoint(", input, StringComparison.Ordinal);
            Assert.Contains(".MovePoint(", input, StringComparison.Ordinal);
            Assert.Contains(".InsertPoint(", input, StringComparison.Ordinal);
            Assert.Contains("MoverPath.AppendCanonicalPoint", adapter, StringComparison.Ordinal);
            Assert.Contains("AntPath.AppendPoint", adapter, StringComparison.Ordinal);
            Assert.DoesNotContain("MoverPath.", input, StringComparison.Ordinal);
            Assert.DoesNotContain("PolylineEditMode", input, StringComparison.Ordinal);
            Assert.DoesNotContain("MoverPath.TruncateCanonicalFrom", input, StringComparison.Ordinal);
            Assert.DoesNotContain("_altCloseHot", rendering, StringComparison.Ordinal);
            Assert.Contains("DrawPolylinePointHandles", rendering, StringComparison.Ordinal);
            Assert.Contains("BeginDocumentEdit?.Invoke();", input, StringComparison.Ordinal);
            Assert.Contains("CompleteDocumentEdit?.Invoke();", input, StringComparison.Ordinal);
        }

        /// <summary>The canvas treats only real polyline movement—not spin's static path—as node-editable.</summary>
        [Theory]
        [InlineData("0,0", false)]
        [InlineData("100,0", true)]
        [InlineData("0,0,100,50", true)]
        [InlineData("RC30", false)]
        public void CanvasPolylineEditingRequiresRealPolylineMovement(string path, bool expected)
        {
            MethodInfo? method = typeof(LevelCanvas).GetMethod(
                "IsEditablePolyline",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            LevelObject obj = new(XElement.Parse($"""<star x="0" y="0" path="{path}" />"""));

            Assert.Equal(expected, method.Invoke(null, [obj]));
        }

        /// <summary>
        /// A grab that orbits draws two concentric circles. The catch ring is hit-tested first everywhere
        /// the orbit is — press, hover cursor, and the applying move branch — so the two radii coinciding
        /// can never make the catch ring unreachable.
        /// </summary>
        [Fact]
        public void CatchRadiusRingOutranksTheOrbitRing()
        {
            string input = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));
            string readout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.DragReadout.cs"));

            Assert.True(
                input.IndexOf("if (OnRadiusEdge(levelPt))", StringComparison.Ordinal)
                    < input.IndexOf("if (OnOrbitEdge(levelPt))", StringComparison.Ordinal),
                "The press handler must offer the catch ring before the orbit ring.");
            Assert.True(
                input.IndexOf(": OnRadiusEdge(levelPt) ? ResizeCursor", StringComparison.Ordinal)
                    < input.IndexOf(": OnOrbitEdge(levelPt) ? ResizeCursor", StringComparison.Ordinal),
                "The hover cursor chain must test the catch ring before the orbit ring.");
            Assert.True(
                input.IndexOf("if (_resizingRadius && SelectedObject is { } g)", StringComparison.Ordinal)
                    < input.IndexOf("if (_resizingOrbit && SelectedObject is { } orbiter)", StringComparison.Ordinal),
                "The move handler must apply the catch ring resize before the orbit resize.");
            Assert.True(
                readout.IndexOf("if (_resizingRadius)", StringComparison.Ordinal)
                    < readout.IndexOf("if (_resizingOrbit)", StringComparison.Ordinal),
                "The badge mapping must follow the same order as the move handler.");
        }

        /// <summary>
        /// Nothing draws the orbit circle while movement paths are hidden — not even for the selected
        /// object, unlike polyline vertices, which keep their own handles. So the ring stops being
        /// grabbable then, rather than leaving an invisible drag target on the canvas.
        /// </summary>
        [Fact]
        public void OrbitRingIsOnlyGrabbableWhileItIsDrawn()
        {
            string input = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));

            int method = input.IndexOf("private bool OnOrbitEdge", StringComparison.Ordinal);
            int end = input.IndexOf("\n        /// <summary>", method, StringComparison.Ordinal);
            string body = input[method..(end < 0 ? input.Length : end)];

            Assert.Contains("ShowMovementPaths", body, StringComparison.Ordinal);
        }

        /// <summary>An orbit resize is one undoable edit, and its flag clears with the gesture.</summary>
        [Fact]
        public void OrbitResizeIsOneUndoableGesture()
        {
            string input = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));

            int gesture = input.IndexOf("private void EndPointerGesture()", StringComparison.Ordinal);
            string body = input[gesture..];

            Assert.Contains("_resizingOrbit = true;", input, StringComparison.Ordinal);
            Assert.Contains("|| _resizingOrbit", body, StringComparison.Ordinal);
            Assert.Contains("_resizingOrbit = false;", body, StringComparison.Ordinal);
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
