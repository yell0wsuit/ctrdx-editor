using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Assigns magic-hat transporter groups, filling open pairs before starting new ones.</summary>
    public static class SockGrouping
    {
        /// <summary>
        /// Returns the group value (as an invariant string) for a newly placed hat: the smallest
        /// existing group with an odd count (an unpaired hat to complete), or else the smallest
        /// non-negative integer not yet used. Entries that are null, non-integer, or negative are
        /// ignored. Empty input yields <c>"0"</c>.
        /// </summary>
        public static string NextGroup(IEnumerable<string?> existingGroups)
        {
            Dictionary<int, int> counts = [];
            foreach (string? value in existingGroups)
            {
                if (int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                    && n >= 0)
                {
                    counts[n] = counts.TryGetValue(n, out int c) ? c + 1 : 1;
                }
            }

            int? smallestOpenPair = counts
                .Where(kv => kv.Value % 2 == 1)
                .Select(kv => (int?)kv.Key)
                .Min();
            if (smallestOpenPair is int open)
            {
                return open.ToString(CultureInfo.InvariantCulture);
            }

            int next = 0;
            while (counts.ContainsKey(next))
            {
                next++;
            }

            return next.ToString(CultureInfo.InvariantCulture);
        }
    }
}
