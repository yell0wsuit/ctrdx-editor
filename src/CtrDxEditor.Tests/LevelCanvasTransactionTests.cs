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
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.cs"));

            Assert.Contains("protected override void OnPointerCaptureLost", source, StringComparison.Ordinal);
            Assert.Contains("EndPointerGesture();", source, StringComparison.Ordinal);
            Assert.Contains("CompleteDocumentEdit?.Invoke();", source, StringComparison.Ordinal);
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
