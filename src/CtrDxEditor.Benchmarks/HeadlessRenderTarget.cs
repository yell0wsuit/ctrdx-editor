using System;
using System.Threading;

using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace CtrDxEditor.Benchmarks
{
    /// <summary>
    /// A real Skia surface to draw into, without a window.
    /// <para>
    /// The headless platform normally stubs out drawing, which would measure nothing; passing
    /// <c>UseHeadlessDrawing = false</c> keeps Skia doing genuine rasterization, so pen tessellation and
    /// fill costs are the same work the desktop editor pays. Avalonia can only be configured once per
    /// process, so <see cref="EnsureAvalonia"/> is idempotent.
    /// </para>
    /// </summary>
    public sealed class HeadlessRenderTarget : IDisposable
    {
        private static readonly Lock BootLock = new();
        private static bool _booted;

        private readonly RenderTargetBitmap _bitmap;

        /// <summary>Creates a surface of the given pixel size, booting Avalonia if it is not already up.</summary>
        /// <param name="width">Surface width in pixels.</param>
        /// <param name="height">Surface height in pixels.</param>
        public HeadlessRenderTarget(int width, int height)
        {
            EnsureAvalonia();
            Width = width;
            Height = height;
            _bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        }

        /// <summary>Surface width in pixels.</summary>
        public int Width { get; }

        /// <summary>Surface height in pixels.</summary>
        public int Height { get; }

        /// <summary>Boots the headless Skia platform once per process.</summary>
        public static void EnsureAvalonia()
        {
            lock (BootLock)
            {
                if (_booted)
                {
                    return;
                }

                _ = AppBuilder.Configure<Application>()
                    .UseSkia()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                    .SetupWithoutStarting();
                _booted = true;
            }
        }

        /// <summary>
        /// Runs <paramref name="draw"/> against a fresh drawing context and disposes it, which is what forces
        /// Skia to actually rasterize. Without the dispose the work could be deferred out of the measurement.
        /// </summary>
        /// <param name="draw">The drawing to perform.</param>
        public void Frame(Action<DrawingContext> draw)
        {
            using DrawingContext ctx = _bitmap.CreateDrawingContext();
            ctx.FillRectangle(Brushes.DimGray, new Rect(0, 0, Width, Height));
            draw(ctx);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _bitmap.Dispose();
        }
    }
}
