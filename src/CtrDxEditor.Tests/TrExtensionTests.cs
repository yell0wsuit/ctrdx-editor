using CtrDxEditor.Localization;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the translation markup extension's access-key handling.</summary>
    public class TrExtensionTests
    {
        /// <summary>The desktop form keeps the marker so AccessText can consume it.</summary>
        [Fact]
        public void DefaultKeepsAccessKeyMarker()
        {
            Assert.Equal("_New...", new TrExtension("Menu.File.New").ProvideValue(null!));
        }

        /// <summary>The touch form strips it, because a tooltip renders it literally.</summary>
        [Fact]
        public void PlainStripsAccessKeyMarker()
        {
            Assert.Equal("Cut", new TrExtension("Menu.Edit.Cut") { Plain = true }.ProvideValue(null!));
        }

        /// <summary>Markers mid-string are stripped too, not just leading ones.</summary>
        [Fact]
        public void PlainStripsMarkersAnywhereInTheString()
        {
            Assert.Equal("Save As...", new TrExtension("Menu.File.SaveAs") { Plain = true }.ProvideValue(null!));
        }
    }
}
