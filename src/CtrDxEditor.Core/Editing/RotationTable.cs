using System.Collections.Generic;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// How a rotatable object's <c>angle</c> attribute maps to its on-screen rotation. The game stores
    /// an angle in degrees and renders the object rotated by <c>angle + DisplayOffset</c> (e.g. pump
    /// <c>+90</c>, rocket <c>-180</c>, most others <c>0</c>). Positive is clockwise, matching the game's
    /// Y-down projection and Avalonia's Y-down screen space.
    /// </summary>
    public sealed record RotationSpec(double DisplayOffset, string AttributeName = "angle", double SnapStep = 15);

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
            ["spike1"] = new RotationSpec(DisplayOffset: 0),
            ["spike2"] = new RotationSpec(DisplayOffset: 0),
            ["spike3"] = new RotationSpec(DisplayOffset: 0),
            ["spike4"] = new RotationSpec(DisplayOffset: 0),
            ["electro"] = new RotationSpec(DisplayOffset: 0),
        };

        /// <summary>The rotation spec for <paramref name="element"/>, or null when it does not rotate.</summary>
        public static RotationSpec? For(string element)
        {
            return Specs.TryGetValue(element, out RotationSpec? spec) ? spec : null;
        }

        /// <summary>Whether <paramref name="element"/> carries a rotation dial.</summary>
        public static bool IsRotatable(string element)
        {
            return Specs.ContainsKey(element);
        }
    }
}
