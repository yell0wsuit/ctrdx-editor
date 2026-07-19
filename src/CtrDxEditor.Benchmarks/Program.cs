using System;
using System.Globalization;

using BenchmarkDotNet.Running;

using CtrDxEditor.Core.Document;

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
        }
    }
}
