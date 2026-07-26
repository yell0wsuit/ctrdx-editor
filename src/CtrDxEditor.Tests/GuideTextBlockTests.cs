using Avalonia.Media;

using CtrDxEditor.Controls;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the font family used to resolve Usage Guide emphasis faces.</summary>
    public class GuideTextBlockTests
    {
        /// <summary>Inherited italics, including screenshot captions, use the bundled Inter family.</summary>
        [Fact]
        public void ControlUsesBundledInterFamily()
        {
            GuideTextBlock block = new();

            Assert.Equal(
                new FontFamily("avares://CtrDxEditor.Shared/Assets/Fonts/Inter/#Inter"),
                block.FontFamily);
        }
    }
}
