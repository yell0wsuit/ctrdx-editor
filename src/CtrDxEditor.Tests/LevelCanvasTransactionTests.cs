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

            Assert.Contains("HitPolylineNub", input, StringComparison.Ordinal);
            Assert.Contains("MoverPath.AppendCanonicalPoint", input, StringComparison.Ordinal);
            Assert.Contains("MoverPath.DeleteCanonicalPoint", input, StringComparison.Ordinal);
            Assert.Contains("MoverPath.MoveCanonicalPoint", input, StringComparison.Ordinal);
            Assert.Contains("MoverPath.InsertCanonicalPoint", input, StringComparison.Ordinal);
            Assert.Contains("MoverPath.CanAddCanonicalPoint", input, StringComparison.Ordinal);
            Assert.DoesNotContain("PolylineEditMode", input, StringComparison.Ordinal);
            Assert.DoesNotContain("MoverPath.TruncateCanonicalFrom", input, StringComparison.Ordinal);
            Assert.DoesNotContain("_altCloseHot", rendering, StringComparison.Ordinal);
            Assert.Contains("DrawPolylinePointHandles", rendering, StringComparison.Ordinal);
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
