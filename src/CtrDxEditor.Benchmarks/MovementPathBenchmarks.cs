using System;
using System.Collections.Generic;
using System.Reflection;

using Avalonia.Media;

using BenchmarkDotNet.Attributes;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Benchmarks
{
    /// <summary>
    /// Measures the movement-path overlay pass, which redraws on every <c>InvalidateVisual</c> — so its cost
    /// lands directly on drag and pan responsiveness, not just on load.
    /// </summary>
    [MemoryDiagnoser]
    public class MovementPathBenchmarks : IDisposable
    {
        private const int SurfaceWidth = 1400;
        private const int SurfaceHeight = 900;
        private const int ObjectCount = 56;

        // LevelSceneRenderer is internal to the Shared assembly. Binding it to a delegate once in setup keeps
        // reflection out of the measured loop, so the numbers are drawing cost rather than dispatch cost.
        private delegate void DrawMovementPathFn(
            DrawingContext ctx, ViewTransform v, LevelObject obj, Pen pathPen, Pen arrowPen, IntSize viewport);

        // Mirrors CanvasPalette's OrbitPath/OrbitPathArrow. The dash pattern is the point of the benchmark:
        // a dotted pen tessellates a segment's whole length, so pen shape drives the cost.
        private static readonly Pen PathPen =
            new(new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), 1.5) { DashStyle = new DashStyle([1, 3], 0) };

        private static readonly Pen ArrowPen =
            new(new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), 2.25);

        private HeadlessRenderTarget _target = null!;
        private DrawMovementPathFn _drawMovementPath = null!;
        private IReadOnlyList<LevelObject> _objects = null!;

        /// <summary>The level shape under test.</summary>
        [Params(LevelShape.OffMapMovers, LevelShape.LocalMovers, LevelShape.DenseStatic)]
        public LevelShape Shape { get; set; }

        /// <summary>Canvas zoom. Cost that grows with zoom means work is being done off-screen.</summary>
        [Params(0.5, 1.0, 2.0)]
        public double Zoom { get; set; }

        /// <summary>Which generated level a run uses.</summary>
        public enum LevelShape
        {
            /// <summary>Movers with sentinel paths running far off the map.</summary>
            OffMapMovers,

            /// <summary>Movers with short on-map paths; the control for path length.</summary>
            LocalMovers,

            /// <summary>Objects with no paths; the floor for object count.</summary>
            DenseStatic,
        }

        /// <summary>Boots Avalonia, binds the internal renderer, and builds the level for this run.</summary>
        [GlobalSetup]
        public void Setup()
        {
            _target = new HeadlessRenderTarget(SurfaceWidth, SurfaceHeight);

            Type renderer = typeof(Rendering.LevelCanvas).Assembly
                .GetType("CtrDxEditor.Rendering.LevelSceneRenderer")
                ?? throw new InvalidOperationException("LevelSceneRenderer not found.");
            MethodInfo method = renderer.GetMethod("DrawMovementPath", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("DrawMovementPath not found.");
            _drawMovementPath = method.CreateDelegate<DrawMovementPathFn>();

            LevelDocument document = Shape switch
            {
                LevelShape.OffMapMovers => StressLevels.OffMapMovers(ObjectCount),
                LevelShape.LocalMovers => StressLevels.LocalMovers(ObjectCount),
                LevelShape.DenseStatic => StressLevels.DenseStatic(ObjectCount),
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

        /// <summary>Draws every object's movement path once, as one editor frame would.</summary>
        [Benchmark]
        public void DrawMovementPaths()
        {
            ViewTransform view = new(Zoom, 200, 100);
            IntSize viewport = new(SurfaceWidth, SurfaceHeight);
            _target.Frame(ctx =>
            {
                foreach (LevelObject obj in _objects)
                {
                    _drawMovementPath(ctx, view, obj, PathPen, ArrowPen, viewport);
                }
            });
        }
    }
}
