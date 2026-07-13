using System.Collections.Generic;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>How an object's rotation pivot is resolved from its authored geometry.</summary>
    public enum RotationCenterKind
    {
        /// <summary>Use the object's XML <c>(x,y)</c> anchor.</summary>
        ObjectAnchor,

        /// <summary>Use the midpoint between a conveyor's anchored and far ends.</summary>
        ConveyorMidpoint,
    }

    /// <summary>
    /// How a rotatable object's <c>angle</c> attribute maps to its on-screen rotation. The game stores
    /// an angle in degrees and renders the object rotated by <c>angle + DisplayOffset</c> (e.g. pump
    /// <c>+90</c>, rocket <c>-180</c>, most others <c>0</c>). Positive is clockwise, matching the game's
    /// Y-down projection and Avalonia's Y-down screen space.
    /// </summary>
    public sealed record RotationSpec(
        double DisplayOffset,
        string AttributeName = "angle",
        double SnapStep = 15,
        bool Editable = true,
        double StoredAngleSign = 1,
        RotationCenterKind CenterKind = RotationCenterKind.ObjectAnchor);

    /// <summary>
    /// Registry of which editor objects carry a rotation dial, keyed by XML element name. Parallels
    /// <see cref="HitboxTable"/> and the visual descriptor map: adding rotation to an object is one row
    /// here plus its <c>angle</c> attribute in the descriptor. UI-free.
    /// </summary>
    public static class RotationTable
    {
        private static readonly Dictionary<string, RotationSpec> Specs = new()
        {
            // Pump: game LoadPumps sets rotation = angle + DEG_90.
            ["pump"] = new RotationSpec(DisplayOffset: 90),
            // SteamTube.InitWithPositionAngle stores and renders the authored angle directly.
            ["steamTube"] = new RotationSpec(DisplayOffset: 0),
            // Tutorial images store and render the authored angle directly.
            ["tutorial01"] = new RotationSpec(DisplayOffset: 0),
            ["tutorial02"] = new RotationSpec(DisplayOffset: 0),
            ["tutorial03"] = new RotationSpec(DisplayOffset: 0),
            ["tutorial04"] = new RotationSpec(DisplayOffset: 0),
            ["tutorial05"] = new RotationSpec(DisplayOffset: 0),
            ["tutorial06"] = new RotationSpec(DisplayOffset: 0),
            ["tutorial07"] = new RotationSpec(DisplayOffset: 0),
            ["tutorial08"] = new RotationSpec(DisplayOffset: 0),
            ["tutorial09"] = new RotationSpec(DisplayOffset: 0),
            ["tutorial10"] = new RotationSpec(DisplayOffset: 0),
            ["tutorial11"] = new RotationSpec(DisplayOffset: 0),
            // Mouse.Update sets the body container rotation to the authored angle directly (no offset);
            // the hole stays upright, so only the body layer is rotated by DrawObject.
            ["gap"] = new RotationSpec(DisplayOffset: 0),
            ["spike1"] = new RotationSpec(DisplayOffset: 0),
            ["spike2"] = new RotationSpec(DisplayOffset: 0),
            ["spike3"] = new RotationSpec(DisplayOffset: 0),
            ["spike4"] = new RotationSpec(DisplayOffset: 0),
            ["electro"] = new RotationSpec(DisplayOffset: 0),
            ["bouncer1"] = new RotationSpec(DisplayOffset: 0),
            ["bouncer2"] = new RotationSpec(DisplayOffset: 0),
            // ConveyorBelt stores positive angles counter-clockwise and renders with rotation = -angle.
            ["transporter"] = new RotationSpec(
                DisplayOffset: 0,
                StoredAngleSign: -1,
                CenterKind: RotationCenterKind.ConveyorMidpoint),
            // CTRGameObject.ParseMover reads the authored sock angle, then LoadSock adds +90.
            // Derived visual keys are thumbnail-only and retain the fixed turn without becoming editable objects.
            ["sock"] = new RotationSpec(DisplayOffset: 90),
            ["sock_grouped"] = new RotationSpec(DisplayOffset: 90, Editable: false),
            ["sock_xmas"] = new RotationSpec(DisplayOffset: 90, Editable: false),
            ["sock_xmas_grouped"] = new RotationSpec(DisplayOffset: 90, Editable: false),
        };

        /// <summary>The rotation spec for <paramref name="element"/>, or null when it does not rotate.</summary>
        public static RotationSpec? For(string element)
        {
            return Specs.TryGetValue(element, out RotationSpec? spec) ? spec : null;
        }

        /// <summary>Whether <paramref name="element"/> carries a rotation dial.</summary>
        public static bool IsRotatable(string element)
        {
            return EditableFor(element) is not null;
        }

        /// <summary>Returns an editable rotation spec, or null for fixed visual turns and unknown elements.</summary>
        /// <param name="element">XML element or visual key to look up.</param>
        /// <returns>The spec only when the canvas may write its angle attribute.</returns>
        public static RotationSpec? EditableFor(string element)
        {
            RotationSpec? spec = For(element);
            return spec?.Editable == true ? spec : null;
        }
    }
}
