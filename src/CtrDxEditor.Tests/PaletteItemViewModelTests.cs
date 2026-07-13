using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests observable state exposed by palette items.</summary>
    public class PaletteItemViewModelTests
    {
        /// <summary>Setting drag state updates the value and raises its property-change notification.</summary>
        [Fact]
        public void IsDraggingRaisesPropertyChangedAndUpdatesValue()
        {
            PaletteItemViewModel item = new("candy", "Candy", enabled: true, icon: null);
            string? changed = null;
            item.PropertyChanged += (_, e) => changed = e.PropertyName;

            Assert.False(item.IsDragging);

            item.IsDragging = true;

            Assert.True(item.IsDragging);
            Assert.Equal(nameof(PaletteItemViewModel.IsDragging), changed);
        }

        /// <summary>Only monochrome tutorial icons request the dark-theme palette treatment.</summary>
        [Theory]
        [InlineData("tutorial01", true)]
        [InlineData("tutorial09", true)]
        [InlineData("tutorial10", false)]
        [InlineData("tutorial11", false)]
        [InlineData("candy", false)]
        public void InvertOnDarkThemeMatchesTutorialIconColorRules(string element, bool expected)
        {
            PaletteItemViewModel item = new(element, "Object", enabled: true, icon: null);

            Assert.Equal(expected, item.InvertOnDarkTheme);
        }
    }
}
