using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Avalonia;

using BenchmarkDotNet.Attributes;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Benchmarks
{
    /// <summary>
    /// Measures the per-frame cull pass and how much drawing it eliminates as zoom rises. Zooming in pushes
    /// most of a level outside the viewport, so a cull that works should discard more objects the further in
    /// the view goes — where before the change every object was drawn at every zoom level.
    /// </summary>
    /// <remarks>
    /// This measures the <em>decision</em>, not the drawing it avoids: the benchmark project ships no art, so
    /// <c>CullBounds</c> takes its no-sprite fallback and every object gets a uniform 16x16 box. The
    /// kept-object counts are therefore the honest signal here; the saved draw cost needs real content and a
    /// device, which is phase 7's measurement.
    /// </remarks>
    [MemoryDiagnoser]
    public class SceneCullingBenchmarks : IDisposable
    {
        private const int SurfaceWidth = 1400;
        private const int SurfaceHeight = 900;
        private const int ObjectCount = 56;

        // Mirrors LevelCanvas.CullMargin. Kept in sync by hand: the constant is private to the Shared
        // assembly, and widening the benchmark's copy alone would silently overstate what culling keeps.
        private const double CullMargin = 256;

        private delegate bool IsWithinViewportFn(
            LevelBounds bounds, ViewTransform view, Size renderSize, double margin);

        private delegate Vec2 DrawOffsetFn(LevelObject obj, double? animationPreviewSeconds);

        private delegate LevelBounds CullBoundsFn(
            SpriteCache sprites,
            LevelObject obj,
            int candySkin,
            int omNomSupport,
            bool nightLevel,
            Vec2 drawOffset);

        private HeadlessRenderTarget _target = null!;
        private IsWithinViewportFn _isWithinViewport = null!;
        private CullBoundsFn _cullBounds = null!;
        private DrawOffsetFn _drawOffset = null!;
        private SpriteCache _sprites = null!;
        private IReadOnlyList<LevelObject> _objects = null!;

        /// <summary>Canvas zoom. With culling, more of the level falls outside the viewport as this rises.</summary>
        [Params(0.5, 1.0, 2.0, 4.0)]
        public double Zoom { get; set; }

        /// <summary>The level shape under test.</summary>
        [Params(LevelShape.DenseStatic, LevelShape.OffMapMovers)]
        public LevelShape Shape { get; set; }

        /// <summary>
        /// Elapsed live-preview seconds, or null for preview off. Preview on is the costlier path: a mover's cull
        /// box has to be walked along its path, which for a circular orbit builds a point per two radius units.
        /// </summary>
        [Params(null, 74.0)]
        public double? PreviewSeconds { get; set; }

        /// <summary>Which generated level a run uses.</summary>
        public enum LevelShape
        {
            /// <summary>Objects with no paths; the floor for object count.</summary>
            DenseStatic,

            /// <summary>Movers with sentinel paths running far off the map.</summary>
            OffMapMovers,
        }

        /// <summary>Boots Avalonia, binds the internal renderer statics, and builds the level for this run.</summary>
        [GlobalSetup]
        public void Setup()
        {
            _target = new HeadlessRenderTarget(SurfaceWidth, SurfaceHeight);
            _sprites = new SpriteCache(new NoContentStore());

            Type renderer = typeof(Rendering.LevelCanvas).Assembly
                .GetType("CtrDxEditor.Rendering.LevelSceneRenderer")
                ?? throw new InvalidOperationException("LevelSceneRenderer not found.");

            MethodInfo cull = renderer.GetMethod("IsWithinViewport", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("IsWithinViewport not found.");
            _isWithinViewport = cull.CreateDelegate<IsWithinViewportFn>();

            MethodInfo bounds = renderer.GetMethod("CullBounds", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("CullBounds not found.");
            _cullBounds = bounds.CreateDelegate<CullBoundsFn>();

            MethodInfo offset = renderer.GetMethod("DrawOffset", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("DrawOffset not found.");
            _drawOffset = offset.CreateDelegate<DrawOffsetFn>();

            LevelDocument document = Shape switch
            {
                LevelShape.DenseStatic => StressLevels.DenseStatic(ObjectCount),
                LevelShape.OffMapMovers => StressLevels.OffMapMovers(ObjectCount),
                _ => throw new ArgumentOutOfRangeException(nameof(Shape)),
            };
            _objects = document.AllObjects;
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

        /// <summary>Runs one frame's cull pass, returning how many objects survived to be drawn.</summary>
        /// <returns>The number of objects the draw loop would render this frame.</returns>
        [Benchmark]
        public int CullPass()
        {
            ViewTransform view = new(Zoom, 200, 100);
            Size renderSize = new(SurfaceWidth, SurfaceHeight);
            int kept = 0;

            foreach (LevelObject obj in _objects)
            {
                LevelBounds bounds = _cullBounds(_sprites, obj, 0, 0, false, _drawOffset(obj, PreviewSeconds));
                if (_isWithinViewport(bounds, view, renderSize, CullMargin))
                {
                    kept++;
                }
            }

            return kept;
        }

        /// <summary>A content store holding nothing, so sprite lookups miss and bounds take their fallback.</summary>
        internal sealed class NoContentStore : IContentStore
        {
            public Task<bool> ExistsAsync(string relPath)
            {
                return Task.FromResult(false);
            }

            public Task<byte[]> ReadBytesAsync(string relPath)
            {
                return Task.FromResult<byte[]>([]);
            }

            public Task<string> ReadTextAsync(string relPath)
            {
                return Task.FromResult(/*lang=json,strict*/ """{"frames":{}}""");
            }

            public Task<bool> IsPopulatedAsync()
            {
                return Task.FromResult(false);
            }
        }
    }
}
