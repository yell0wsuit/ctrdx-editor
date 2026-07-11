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
            // Om Nom
            new ObjectDescriptor("target", "Om Nom", [], MaxCount: int.MaxValue),

            // Candy
            new ObjectDescriptor("candy", "Candy", [], MaxCount: int.MaxValue),
            new ObjectDescriptor("candyL", "Candy (Left)", [], MaxCount: 1),
            new ObjectDescriptor("candyR", "Candy (Right)", [], MaxCount: 1),

            // Star
            new ObjectDescriptor("star", "Star",
            [
                new AttributeSpec("timeout", AttrType.Number, "-1"),
            ], MaxCount: int.MaxValue),

            // Rope hook
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

            // Bubble
            new ObjectDescriptor("bubble", "Bubble", [], MaxCount: int.MaxValue),

            // Spike
            new ObjectDescriptor("spike1", "Spike",
            [
                new AttributeSpec("angle", AttrType.Number, "0"),
                new AttributeSpec("size", AttrType.Enum, "1", EnumValues: ["1", "2", "3", "4"]),
                new AttributeSpec("toggled", AttrType.Bool, "false"),
            ], MaxCount: int.MaxValue, LocalizationName: "spike"),
            new ObjectDescriptor("spike2", "Spike",
            [
                new AttributeSpec("angle", AttrType.Number, "0"),
                new AttributeSpec("size", AttrType.Enum, "2", EnumValues: ["1", "2", "3", "4"]),
                new AttributeSpec("toggled", AttrType.Bool, "false"),
            ], MaxCount: int.MaxValue, LocalizationName: "spike"),
            new ObjectDescriptor("spike3", "Spike",
            [
                new AttributeSpec("angle", AttrType.Number, "0"),
                new AttributeSpec("size", AttrType.Enum, "3", EnumValues: ["1", "2", "3", "4"]),
                new AttributeSpec("toggled", AttrType.Bool, "false"),
            ], MaxCount: int.MaxValue, LocalizationName: "spike"),
            new ObjectDescriptor("spike4", "Spike",
            [
                new AttributeSpec("angle", AttrType.Number, "0"),
                new AttributeSpec("size", AttrType.Enum, "4", EnumValues: ["1", "2", "3", "4"]),
                new AttributeSpec("toggled", AttrType.Bool, "false"),
            ], MaxCount: int.MaxValue, LocalizationName: "spike"),

            // Air cushion
            new ObjectDescriptor("pump", "Pump",
            [
                new AttributeSpec("angle", AttrType.Number, "0"),
            ], MaxCount: int.MaxValue),

            // Electric spark
            new ObjectDescriptor("electro", "Electro",
            [
                new AttributeSpec("initialDelay", AttrType.Number, "0.0"),
                new AttributeSpec("offTime", AttrType.Number, "2.0"),
                new AttributeSpec("onTime", AttrType.Number, "2.0"),
                new AttributeSpec("angle", AttrType.Number, "0"),
            ], MaxCount: int.MaxValue),

            // Magic hat teleporter (Christmas sock during the seasonal event)
            new ObjectDescriptor("sock", "Magic Hat",
            [
                new AttributeSpec(
                    "group",
                    AttrType.Whole,
                    "0",
                    LocalizationName: "sockGroup"),
                new AttributeSpec("angle", AttrType.Number, "0"),
            ], MaxCount: int.MaxValue),

            // Bouncers
            new ObjectDescriptor("bouncer1", "Bouncer",
            [
                new AttributeSpec("size", AttrType.Enum, "1", EnumValues: ["1", "2"]),
                new AttributeSpec("angle", AttrType.Number, "0"),
            ], MaxCount: int.MaxValue, LocalizationName: "bouncer"),
            new ObjectDescriptor("bouncer2", "Bouncer",
            [
                new AttributeSpec("size", AttrType.Enum, "2", EnumValues: ["1", "2"]),
                new AttributeSpec("angle", AttrType.Number, "0"),
            ], MaxCount: int.MaxValue, LocalizationName: "bouncer"),

            // Gravity button
            new ObjectDescriptor("gravitySwitch", "Gravity Switch", [], MaxCount: int.MaxValue),

            // Light bulb
            new ObjectDescriptor("lightBulb", "Light Bulb",
            [
                new AttributeSpec("litRadius", AttrType.Whole, "50"),
            ], MaxCount: int.MaxValue),
        ]);
    }
}
