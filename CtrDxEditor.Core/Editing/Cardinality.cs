using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Helpers for enforcing per-object placement limits.</summary>
    public static class Cardinality
    {
        /// <summary>Counts objects matching <paramref name="elementName"/>.</summary>
        public static int CountOf(string elementName, IReadOnlyList<LevelObject> objects)
        {
            return objects.Count(o => o.Type == elementName);
        }

        /// <summary>Returns whether <paramref name="descriptor"/> has reached its maximum count.</summary>
        public static bool IsAtCapacity(ObjectDescriptor descriptor, IReadOnlyList<LevelObject> objects)
        {
            return CountOf(descriptor.ElementName, objects) >= descriptor.MaxCount;
        }
    }
}
