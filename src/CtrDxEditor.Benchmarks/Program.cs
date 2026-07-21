using System;
using System.Globalization;
using System.Reflection;

using Avalonia;

using BenchmarkDotNet.Running;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Benchmarks
{
    /// <summary>Entry point for the render benchmarks.</summary>
    public static class Program
    {
        /// <summary>Runs the benchmarks, or prints fixture sizes with <c>--describe</c>.</summary>
        /// <param name="args">Command line arguments, forwarded to BenchmarkDotNet.</param>
        public static void Main(string[] args)
        {
            if (Array.Exists(args, a => a == "--describe"))
            {
                Describe();
                return;
            }

            _ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }

        // Prints how much path each generated level actually contains, so a fixture that has drifted away
        // from the workload it is meant to represent is visible without running a full benchmark.
        private static void Describe()
        {
            (string Name, LevelDocument Document)[] levels =
            [
                ("OffMapMovers(56)", StressLevels.OffMapMovers(56)),
                ("LocalMovers(56)", StressLevels.LocalMovers(56)),
                ("DenseStatic(56)", StressLevels.DenseStatic(56)),
            ];

            foreach ((string name, LevelDocument document) in levels)
            {
                double length = StressLevels.TotalPathLength(document);
                Console.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0,-18} objects={1,4}  total path={2,12:N0} level units",
                    name,
                    document.AllObjects.Count,
                    length));
            }

            DescribeCulling();
        }

        // Prints how many objects survive the viewport cull at each zoom. A count that does not fall as zoom
        // rises means the cull is not discarding anything, which is the failure the optimization is meant to
        // prevent — visible here without reading a timing table.
        private static void DescribeCulling()
        {
            Console.WriteLine();
            Console.WriteLine("Viewport cull — objects kept of 56 (1400x900 surface, pan 200,100):");

            SpriteCache sprites = new(new SceneCullingBenchmarks.NoContentStore());
            Type renderer = typeof(Rendering.LevelCanvas).Assembly
                .GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;
            MethodInfo boundsMethod = renderer.GetMethod(
                "SelectionBounds", BindingFlags.Public | BindingFlags.Static)!;
            MethodInfo cullMethod = renderer.GetMethod(
                "IsWithinViewport", BindingFlags.Public | BindingFlags.Static)!;

            (string Name, LevelDocument Document)[] shapes =
            [
                ("DenseStatic", StressLevels.DenseStatic(56)),
                ("OffMapMovers", StressLevels.OffMapMovers(56)),
            ];

            foreach ((string shapeName, LevelDocument document) in shapes)
            {
                foreach (double zoom in (double[])[0.5, 1.0, 2.0, 4.0])
                {
                    ViewTransform view = new(zoom, 200, 100);
                    int kept = 0;
                    foreach (LevelObject obj in document.AllObjects)
                    {
                        LevelBounds bounds =
                            (LevelBounds)boundsMethod.Invoke(null, [sprites, obj, 0, 0, false])!;
                        if ((bool)cullMethod.Invoke(null, [bounds, view, new Size(1400, 900), 256.0])!)
                        {
                            kept++;
                        }
                    }

                    Console.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "  {0,-13} zoom={1,-4} kept={2,3}",
                        shapeName,
                        zoom,
                        kept));
                }
            }
        }
    }
}
