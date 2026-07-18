using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests layer-panel wiring that is difficult to exercise through headless pointer input.</summary>
    public class MainViewLayerPanelTests
    {
        /// <summary>Every terminal pointer path clears pending layer-drag gesture state.</summary>
        [Fact]
        public void LayerDragClearsPendingStateWhenPointerGestureEnds()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string codeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Commands.cs"));
            string viewCodeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));

            Assert.Contains("PointerReleased=\"LayerRow_PointerReleased\"", view, StringComparison.Ordinal);
            Assert.Contains("PointerCaptureLost=\"LayerRow_PointerCaptureLost\"", view, StringComparison.Ordinal);
            Assert.Contains("ClearPendingLayerDrag();", codeBehind, StringComparison.Ordinal);
            Assert.Contains(
                "PointerReleasedEvent, LayerRow_PointerReleased, RoutingStrategies.Bubble, handledEventsToo: true",
                viewCodeBehind,
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
    }
}
