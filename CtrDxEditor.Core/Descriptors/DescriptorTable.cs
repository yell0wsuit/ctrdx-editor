using System.Collections.Generic;
using System.Linq;

namespace CtrDxEditor.Core.Descriptors
{
    public sealed class DescriptorTable(IReadOnlyList<ObjectDescriptor> descriptors)
    {
        public IReadOnlyDictionary<string, ObjectDescriptor> ByElement { get; } =
            descriptors.ToDictionary(d => d.ElementName);

        public bool Knows(string elementName)
        {
            return ByElement.ContainsKey(elementName);
        }

        public ObjectDescriptor? For(string elementName)
        {
            return ByElement.TryGetValue(elementName, out ObjectDescriptor? d) ? d : null;
        }

        public static DescriptorTable Default { get; } = new(
        [
            new ObjectDescriptor("target", "Om Nom", [], MaxCount: 1),
            new ObjectDescriptor("candy", "Candy", [], MaxCount: 1),
            new ObjectDescriptor("star", "Star",
            [
                new AttributeSpec("timeout", AttrType.Int, "-1"),
            ], MaxCount: int.MaxValue),
            new ObjectDescriptor("grab", "Grab",
            [
                new AttributeSpec("length", AttrType.Int, "100"),
                new AttributeSpec("part", AttrType.Enum, null, EnumValues: ["L", "R"]),
                new AttributeSpec("wheel", AttrType.Bool, "false"),
                new AttributeSpec("gun", AttrType.Bool, "false"),
                new AttributeSpec("radius", AttrType.Int, "-1"),
                new AttributeSpec("moveLength", AttrType.Int, "-1"),
                new AttributeSpec("moveVertical", AttrType.Bool, "false"),
                new AttributeSpec("moveOffset", AttrType.Int, "0"),
                new AttributeSpec("spider", AttrType.Bool, "false"),
            ], MaxCount: int.MaxValue),
        ]);
    }
}
