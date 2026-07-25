using CtrDxEditor.UsageGuide;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the replaceable screenshot-slot presentation state.</summary>
    public class GuideScreenshotStateTests
    {
        /// <summary>An unfilled screenshot slot renders its informative placeholder.</summary>
        /// <param name="source">Missing or whitespace-only screenshot resource URI.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void MissingSourceShowsPlaceholder(string? source)
        {
            GuideScreenshotState state = GuideScreenshotState.From(source);

            Assert.True(state.ShowPlaceholder);
            Assert.False(state.ShowImage);
        }

        /// <summary>Adding an embedded source replaces the placeholder with the image.</summary>
        [Fact]
        public void SuppliedSourceShowsImage()
        {
            GuideScreenshotState state = GuideScreenshotState.From("/Assets/Guide/place-object.png");

            Assert.False(state.ShowPlaceholder);
            Assert.True(state.ShowImage);
        }
    }
}
