using System;
using System.Threading.Tasks;

using Avalonia;

using BenchmarkDotNet.Attributes;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Rendering;

namespace CtrDxEditor.Benchmarks
{
    /// <summary>
    /// Measures one whole <see cref="LevelCanvas.Render"/> call, the frame every pan, zoom and drag pays. Where
    /// the other benchmarks isolate a single pass, this one catches per-frame work hiding anywhere in the canvas.
    /// </summary>
    /// <remarks>
    /// Without content the sprite lookups miss and no art is drawn, but the per-object walk still runs, so object
    /// count costs stay visible. Set <c>CTRDX_CONTENT</c> to an installed content folder (the one holding
    /// <c>images/</c>) to draw real art.
    /// </remarks>
    [MemoryDiagnoser]
    public class SceneRenderBenchmarks : IDisposable
    {
        private const int SurfaceWidth = 1400;
        private const int SurfaceHeight = 900;
        private const double Zoom = 1.5;
        private const int Count = 600;

        private HeadlessRenderTarget _target = null!;
        private LevelCanvas _canvas = null!;

        /// <summary>
        /// The object stacked 600 times. <see cref="StressLevels.StackedKind.Star"/> does no cross-object work, so a
        /// kind that costs well above it is doing per-object work that scales with the level.
        /// </summary>
        [ParamsAllValues]
        public StressLevels.StackedKind Kind { get; set; }

        /// <summary>
        /// Whether the level sits inside the surface. Off screen, Skia rejects every draw before rasterizing,
        /// which leaves the editor's own per-frame work; the gap to on screen is rasterization.
        /// </summary>
        [Params(true, false)]
        public bool OnScreen { get; set; }

        /// <summary>Boots Avalonia, loads content when configured, and lays out a canvas on the level.</summary>
        [GlobalSetup]
        public void Setup()
        {
            _target = new HeadlessRenderTarget(SurfaceWidth, SurfaceHeight);

            string? contentRoot = Environment.GetEnvironmentVariable("CTRDX_CONTENT");
            SpriteCache sprites;
            if (string.IsNullOrEmpty(contentRoot))
            {
                sprites = new SpriteCache(new SceneCullingBenchmarks.NoContentStore());
            }
            else
            {
                sprites = new SpriteCache(new FolderContentStore(contentRoot));
                // Off the setup thread: the headless platform installs a synchronization context, and blocking
                // on an await that resumes there would deadlock.
                Task.Run(sprites.PreloadAsync).GetAwaiter().GetResult();
                // A wrong path preloads nothing and would quietly benchmark the art-free frame instead.
                if (sprites.GetSprite("grab") is null)
                {
                    throw new InvalidOperationException($"CTRDX_CONTENT '{contentRoot}' has no grab art.");
                }
            }
            Console.WriteLine($"// SceneRender content: {(string.IsNullOrEmpty(contentRoot) ? "none" : contentRoot)}");

            _canvas = new LevelCanvas
            {
                Document = StressLevels.Stacked(Kind, Count),
                Sprites = sprites,
            };
            Size size = new(SurfaceWidth, SurfaceHeight);
            _canvas.Measure(size);
            _canvas.Arrange(new Rect(size));
            // After layout: the first arrange fits the level to the surface, replacing any view set before it.
            _canvas.View = new ViewTransform(Zoom, OnScreen ? 300 : 100_000, 50);
        }

        /// <summary>Releases the render surface.</summary>
        [GlobalCleanup]
        public void Cleanup()
        {
            Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _target?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>Renders the canvas once, as a single editor frame.</summary>
        [Benchmark]
        public void RenderFrame()
        {
            _target.Frame(_canvas.Render);
        }
    }
}
