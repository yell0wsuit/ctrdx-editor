using System;
using System.Globalization;
using System.Text;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Benchmarks
{
    /// <summary>
    /// Builds level documents in code so the benchmarks depend on no level files. Each shape isolates one
    /// cost: <see cref="OffMapMovers"/> varies path length, <see cref="LocalMovers"/> holds the mover count
    /// but not the length, and <see cref="DenseStatic"/> holds the object count but has no paths at all.
    /// Comparing the three separates "how much path" from "how many objects".
    /// </summary>
    public static class StressLevels
    {
        private const int Width = 853;
        private const int Height = 2880;

        /// <summary>
        /// Movers whose paths run far off the map, the shape that made the editor lag. Real levels express
        /// "travel off-screen" with sentinel offsets in the ±10000 range, so a handful of objects can add up
        /// to over a million level units of path.
        /// </summary>
        /// <param name="count">How many movers to place.</param>
        /// <returns>A parsed level document.</returns>
        public static LevelDocument OffMapMovers(int count)
        {
            StringBuilder objects = new();
            for (int i = 0; i < count; i++)
            {
                int column = i % 8;
                int row = i / 8;
                int x = 100 + (column * 100);
                int y = 200 + (row * 200);
                // Alternate the sentinel direction so segments run vertically, horizontally and diagonally,
                // rather than all overlapping into one span the rasterizer could treat as a single line.
                string path = (i % 3) switch
                {
                    0 => "0,-10734",
                    1 => "-9999,0",
                    _ => "-9999,-9999",
                };
                _ = objects.Append(CultureInfo.InvariantCulture, $"""
                        <bouncer1 x="{x}" y="{y}" angle="0" size="2" path="{path}" moveSpeed="100" />

                    """);
            }

            return Build(objects.ToString());
        }

        /// <summary>
        /// The control for <see cref="OffMapMovers"/>: the same mover count and the same per-object work,
        /// but short paths that stay on the map. The gap between the two is the cost of path length alone.
        /// </summary>
        /// <param name="count">How many movers to place.</param>
        /// <returns>A parsed level document.</returns>
        public static LevelDocument LocalMovers(int count)
        {
            StringBuilder objects = new();
            for (int i = 0; i < count; i++)
            {
                int column = i % 8;
                int row = i / 8;
                int x = 100 + (column * 100);
                int y = 200 + (row * 200);
                _ = objects.Append(CultureInfo.InvariantCulture, $"""
                        <bouncer1 x="{x}" y="{y}" angle="0" size="2" path="120,0,120,120,0,120" moveSpeed="100" />

                    """);
            }

            return Build(objects.ToString());
        }

        /// <summary>
        /// Objects with no movement at all, so the movement-path pass walks the same object count but draws
        /// nothing. This is the floor the other two shapes are measured against.
        /// </summary>
        /// <param name="count">How many objects to place.</param>
        /// <returns>A parsed level document.</returns>
        public static LevelDocument DenseStatic(int count)
        {
            StringBuilder objects = new();
            for (int i = 0; i < count; i++)
            {
                int column = i % 16;
                int row = i / 16;
                int x = 40 + (column * 50);
                int y = 100 + (row * 120);
                _ = objects.Append(CultureInfo.InvariantCulture, $"""
                        <spike2 x="{x}" y="{y}" angle="0" size="3" />

                    """);
            }

            return Build(objects.ToString());
        }

        private static LevelDocument Build(string objectElements)
        {
            string xml = $"""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="{Width}" height="{Height}" />
                        <gameDesign ropePhysicsSpeed="1" twoParts="false" nightLevel="false" />
                    </layer>
                    <layer name="Objects">
                {objectElements}    </layer>
                </map>
                """;
            return LevelDocument.Parse(xml);
        }

        /// <summary>Total length of every path segment the movement-path pass will draw, in level units.</summary>
        /// <param name="document">The level to measure.</param>
        /// <returns>The summed segment length, useful for sanity-checking that a fixture is as heavy as intended.</returns>
        public static double TotalPathLength(LevelDocument document)
        {
            double total = 0;
            foreach (LevelObject obj in document.AllObjects)
            {
                Core.Geometry.Vec2[] points = Core.Editing.MoverPath.Points(
                    new Core.Geometry.Vec2(obj.X, obj.Y), obj.GetAttr("path"));
                for (int i = 0; i < points.Length; i++)
                {
                    Core.Geometry.Vec2 a = points[i];
                    Core.Geometry.Vec2 b = points[(i + 1) % points.Length];
                    total += Math.Sqrt(((b.X - a.X) * (b.X - a.X)) + ((b.Y - a.Y) * (b.Y - a.Y)));
                }
            }

            return total;
        }
    }
}
