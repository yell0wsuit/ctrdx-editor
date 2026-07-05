using System.Collections.Generic;
using System.Globalization;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Assigns 0-based integer keys, filling the smallest unused slot.</summary>
    public static class KeyNumbering
    {
        /// <summary>
        /// Returns the smallest non-negative integer (as an invariant string) not already used by
        /// <paramref name="existingKeys"/>. Keys that are null or do not parse as non-negative
        /// integers are ignored. Empty input yields <c>"0"</c>.
        /// </summary>
        public static string NextKey(IEnumerable<string?> existingKeys)
        {
            HashSet<int> used = [];
            foreach (string? key in existingKeys)
            {
                if (key is not null
                    && int.TryParse(key.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                    && n >= 0)
                {
                    _ = used.Add(n);
                }
            }

            int next = 0;
            while (used.Contains(next))
            {
                next++;
            }

            return next.ToString(CultureInfo.InvariantCulture);
        }
    }
}
