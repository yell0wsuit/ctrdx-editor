using System;
using System.IO;

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

        /// <summary>Movement path visibility is exposed through the View menu and bound into the canvas.</summary>
        [Fact]
        public void ViewMenuWiresMovementPathToggleToCanvas()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string codeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));

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

            Assert.DoesNotContain("PolylineEditMode", view, StringComparison.Ordinal);
            Assert.DoesNotContain("PolylineEditMode", vm, StringComparison.Ordinal);
            Assert.DoesNotContain("Menu.Edit.PolylineEditPoints", view, StringComparison.Ordinal);
            Assert.Contains("CanEditPolyline", vm, StringComparison.Ordinal);
        }

        /// <summary>Polyline gestures append via the nub and delete single vertices; the old mode/truncate paths are gone.</summary>
        [Fact]
        public void PolylineGesturesUseNubAppendAndSingleVertexDelete()
        {
            string input = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));
            string rendering = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Rendering.cs"));

            Assert.Contains("HitPolylineNub", input, StringComparison.Ordinal);
            Assert.Contains("MoverPath.AppendCanonicalPoint", input, StringComparison.Ordinal);
            Assert.Contains("MoverPath.DeleteCanonicalPoint", input, StringComparison.Ordinal);
            Assert.Contains("MoverPath.MoveCanonicalPoint", input, StringComparison.Ordinal);
            Assert.Contains("MoverPath.InsertCanonicalPoint", input, StringComparison.Ordinal);
            Assert.DoesNotContain("PolylineEditMode", input, StringComparison.Ordinal);
            Assert.DoesNotContain("MoverPath.TruncateCanonicalFrom", input, StringComparison.Ordinal);
            Assert.DoesNotContain("_altCloseHot", rendering, StringComparison.Ordinal);
            Assert.Contains("DrawPolylinePointHandles", rendering, StringComparison.Ordinal);
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
