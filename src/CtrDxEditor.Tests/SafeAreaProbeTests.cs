using System;

using Avalonia;
using Avalonia.Controls;

using CtrDxEditor.Views;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests safe-area inset resolution and its platform-source override.</summary>
    public class SafeAreaProbeTests : IDisposable
    {
        /// <summary>Clears the static override so tests cannot leak into each other.</summary>
        public void Dispose()
        {
            SafeAreaProbe.PlatformSource = null;
            GC.SuppressFinalize(this);
        }

        /// <summary>A control with no TopLevel reports zero insets rather than throwing.</summary>
        [Fact]
        public void DetachedControlReportsZero()
        {
            Border detached = new();

            Assert.Equal(new Thickness(0), SafeAreaProbe.Read(detached));
        }

        /// <summary>
        /// The platform source wins over InsetsManager, which reports wrong values on iOS Safari.
        /// </summary>
        [Fact]
        public void PlatformSourceTakesPrecedence()
        {
            SafeAreaProbe.PlatformSource = () => new Thickness(48, 0, 48, 20);
            Border detached = new();

            Assert.Equal(new Thickness(48, 0, 48, 20), SafeAreaProbe.Read(detached));
        }

        /// <summary>Clearing the platform source falls back to the platform manager path.</summary>
        [Fact]
        public void ClearedPlatformSourceFallsBack()
        {
            SafeAreaProbe.PlatformSource = () => new Thickness(48, 0, 48, 20);
            SafeAreaProbe.PlatformSource = null;
            Border detached = new();

            Assert.Equal(new Thickness(0), SafeAreaProbe.Read(detached));
        }

        /// <summary>
        /// A throwing platform source degrades to zero rather than taking the app down. The source calls
        /// into JavaScript, which can fail on an unexpected host.
        /// </summary>
        [Fact]
        public void ThrowingPlatformSourceDegradesToZero()
        {
            SafeAreaProbe.PlatformSource = () => throw new InvalidOperationException("js unavailable");
            Border detached = new();

            Assert.Equal(new Thickness(0), SafeAreaProbe.Read(detached));
        }
    }
}
