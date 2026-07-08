using System.Collections.Generic;
using System.Linq;

namespace CtrDxEditor.Core.Descriptors
{
    /// <summary>Lookup table of editable object descriptors keyed by XML element name.</summary>
    public sealed class DescriptorTable(IReadOnlyList<ObjectDescriptor> descriptors)
    {
        /// <summary>Descriptors keyed by their XML element name.</summary>
        public IReadOnlyDictionary<string, ObjectDescriptor> ByElement { get; } =
            descriptors.ToDictionary(d => d.ElementName);

        /// <summary>Returns whether the table contains a descriptor for <paramref name="elementName"/>.</summary>
        public bool Knows(string elementName)
        {
            return ByElement.ContainsKey(elementName);
        }

        /// <summary>Returns the descriptor for <paramref name="elementName"/>, or null when unknown.</summary>
        public ObjectDescriptor? For(string elementName)
        {
            return ByElement.TryGetValue(elementName, out ObjectDescriptor? d) ? d : null;
        }

        /// <summary>Built-in descriptor set for the currently supported editor objects.</summary>
        public static DescriptorTable Default { get; } = new(
        [
            new ObjectDescriptor("target", "Om Nom", [], MaxCount: int.MaxValue),
            new ObjectDescriptor("candy", "Candy",
            [
                new AttributeSpec("candyNumber", AttrType.Text, null),
            ], MaxCount: int.MaxValue),
            new ObjectDescriptor("candyL", "Candy (Left)", [], MaxCount: 1),
            new ObjectDescriptor("candyR", "Candy (Right)", [], MaxCount: 1),
            new ObjectDescriptor("star", "Star",
            [
                new AttributeSpec("timeout", AttrType.Number, "-1"),
            ], MaxCount: int.MaxValue),
            new ObjectDescriptor("grab", "Grab",
            [
                new AttributeSpec("length", AttrType.Whole, "100"),
                new AttributeSpec("part", AttrType.Enum, null, EnumValues: ["L", "R"]),
                new AttributeSpec("wheel", AttrType.Bool, "false"),
                new AttributeSpec("gun", AttrType.Bool, "false"),
                new AttributeSpec("radius", AttrType.Whole, "-1"),
                new AttributeSpec("moveLength", AttrType.Whole, "-1"),
                new AttributeSpec("moveVertical", AttrType.Bool, "false"),
                new AttributeSpec("moveOffset", AttrType.Whole, "0"),
                new AttributeSpec("spider", AttrType.Bool, "false"),
                new AttributeSpec("kickable", AttrType.Bool, "false"),
                new AttributeSpec("kicked", AttrType.Bool, "false"),
                new AttributeSpec("invisible", AttrType.Bool, "false"),
            ], MaxCount: int.MaxValue),
            new ObjectDescriptor("bubble", "Bubble", [], MaxCount: int.MaxValue),
            new ObjectDescriptor("gravitySwitch", "Gravity Switch", [], MaxCount: int.MaxValue),
            new ObjectDescriptor("lightBulb", "Light Bulb",
            [
                new AttributeSpec("litRadius", AttrType.Whole, "50"),
                new AttributeSpec("bulbNumber", AttrType.Text, null),
            ], MaxCount: int.MaxValue),
            new ObjectDescriptor("pump", "Pump",
            [
                new AttributeSpec("angle", AttrType.Number, "0"),
            ], MaxCount: int.MaxValue),
        ]);
    }
}
