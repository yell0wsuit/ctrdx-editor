using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Editing;

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
        public static DescriptorTable CtrObjects { get; } = new(
        [
            // Om Nom
            new ObjectDescriptor("target", "Om Nom", [], MaxCount: int.MaxValue),

            // Candy
            new ObjectDescriptor("candy", "Candy", [], MaxCount: int.MaxValue),
            new ObjectDescriptor("candyL", "Candy (left)", [], MaxCount: 1),
            new ObjectDescriptor("candyR", "Candy (right)", [], MaxCount: 1),

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
                new AttributeSpec("hidePath", AttrType.Bool, "false"),
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
            new ObjectDescriptor("pump", "Air cushion",
            [
                new AttributeSpec("angle", AttrType.Number, "0"),
            ], MaxCount: int.MaxValue),

            // Electric spark
            new ObjectDescriptor("electro", "Electric spark",
            [
                new AttributeSpec("initialDelay", AttrType.Number, "0.0"),
                new AttributeSpec("offTime", AttrType.Number, "2.0"),
                new AttributeSpec("onTime", AttrType.Number, "2.0"),
                new AttributeSpec("angle", AttrType.Number, "0"),
            ], MaxCount: int.MaxValue),

            // Magic hat teleporter (Christmas sock during the seasonal event)
            new ObjectDescriptor("sock", "Magic hat",
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
            new ObjectDescriptor("gravitySwitch", "Gravity switch", [], MaxCount: int.MaxValue),

            // Vinyl (rotating disc / DJ disc). Game element rotatedCircle; size is the disc radius,
            // handleAngle rotates the 1-2 handles, oneHandle hides the left handle.
            new ObjectDescriptor("rotatedCircle", "Vinyl",
            [
                new AttributeSpec("size", AttrType.Whole, "110"),
                new AttributeSpec("handleAngle", AttrType.Number, "0"),
                new AttributeSpec("oneHandle", AttrType.Bool, "false"),
            ], MaxCount: int.MaxValue, LocalizationName: "vinyl"),

            // Ghost
            new ObjectDescriptor("ghost", "Ghost",
            [
                new AttributeSpec("grab", AttrType.Bool, "true"),
                new AttributeSpec("bubble", AttrType.Bool, "false"),
                new AttributeSpec("bouncer", AttrType.Bool, "false"),
                new AttributeSpec("radius", AttrType.Whole, "50"),
                new AttributeSpec("angle", AttrType.Number, "0"),
            ], MaxCount: int.MaxValue),

            // Steam pipe
            new ObjectDescriptor("steamTube", "Steam pipe",
            [
                new AttributeSpec("angle", AttrType.Number, "0"),
            ], MaxCount: int.MaxValue),

             // Lantern
            new ObjectDescriptor("lantern", "Lantern",
            [
                new AttributeSpec("candyCaptured", AttrType.Bool, "false"),
            ], MaxCount: int.MaxValue),

             // Mouse
            new ObjectDescriptor("gap", "Mouse",
            [
                new AttributeSpec("angle", AttrType.Number, "0"),
                new AttributeSpec("radius", AttrType.Number, "50"),
                new AttributeSpec("activeTime", AttrType.Number, "1.0"),
                new AttributeSpec("index", AttrType.Whole, null),
            ], MaxCount: int.MaxValue),

            // Light bulb
            new ObjectDescriptor("lightBulb", "Light bulb",
            [
                new AttributeSpec("litRadius", AttrType.Whole, "50"),
            ], MaxCount: int.MaxValue),

            // Conveyor belt
            new ObjectDescriptor("transporter", "Conveyor",
            [
                new AttributeSpec("velocity", AttrType.Number, ConveyorObject.DefaultVelocity),
                new AttributeSpec("direction", AttrType.Enum, ConveyorObject.DefaultDirection, EnumValues: ["forward", "backward"]),
                new AttributeSpec("length", AttrType.Number, ConveyorObject.DefaultLength),
                new AttributeSpec("width", AttrType.Number, ConveyorObject.DefaultWidth),
                new AttributeSpec("angle", AttrType.Number, ConveyorObject.DefaultAngle),
                new AttributeSpec("type", AttrType.Enum, null, EnumValues: ["manual"]),
            ], MaxCount: int.MaxValue),
        ]);
    }
}
