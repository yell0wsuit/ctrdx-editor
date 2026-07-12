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
    }
}
