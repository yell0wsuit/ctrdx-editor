using System.Collections.Generic;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    public static class HitTester
    {
        /// <summary>
        /// Bounds are back-to-front (last = topmost). Returns the topmost index containing
        /// <paramref name="point"/>. With <paramref name="afterIndex"/> &gt;= 0, continues the search
        /// downward from just below that index, wrapping - used to cycle co-located objects.
        /// </summary>
        public static int Topmost(IReadOnlyList<LevelBounds> bounds, Vec2 point, int afterIndex = -1)
        {
            int n = bounds.Count;
            if (n == 0)
            {
                return -1;
            }

            int start = afterIndex >= 0 ? afterIndex - 1 + n : n - 1;
            for (int step = 0; step < n; step++)
            {
                int i = (start - step) % n;
                if (bounds[i].Contains(point))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
