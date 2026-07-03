using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    public static class Cardinality
    {
        public static int CountOf(string elementName, IReadOnlyList<LevelObject> objects)
        {
            return objects.Count(o => o.Type == elementName);
        }

        public static bool IsAtCapacity(ObjectDescriptor descriptor, IReadOnlyList<LevelObject> objects)
        {
            return CountOf(descriptor.ElementName, objects) >= descriptor.MaxCount;
        }
    }
}
