using Avalonia;
using Avalonia.Controls;

using CtrDxEditor.Views;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the safe-area probe's fallbacks on platforms that report no insets.</summary>
    public class SafeAreaProbeTests
    {
        /// <summary>A control with no TopLevel reports zero insets rather than throwing.</summary>
        [Fact]
        public void DetachedControlReportsZero()
        {
            Border detached = new();

            Assert.Equal(new Thickness(0), SafeAreaProbe.Read(detached));
        }
    }
}
