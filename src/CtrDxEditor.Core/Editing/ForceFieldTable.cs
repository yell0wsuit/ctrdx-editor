using System.Collections.Generic;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// A directional force emitter's field: how far it pushes and which way relative to the object's
    /// on-screen rotation. <see cref="Reach"/> is in game units (the same space as <see cref="HitboxDef"/>
    /// boxes), converted to level units by the sprite's <c>scale / mapScale</c> like <see cref="HitboxTable"/>.
    /// <see cref="DirectionOffset"/> is added to the object's display angle to get the push direction, so a
    /// pump that blows along its mouth (game impulse <c>rotate((0,-1), rotation)</c>) is <c>-90</c>.
    /// </summary>
    public sealed record ForceFieldSpec(double Reach, double DirectionOffset);

    /// <summary>
    /// Registry of objects that emit a directional force, keyed by XML element name. Parallels
    /// <see cref="RotationTable"/> and <see cref="HitboxTable"/>: the force overlay reads the reach and
    /// direction from here, so adding an emitter (steam later) is one row. UI-free.
    /// </summary>
    public static class ForceFieldTable
    {
        private static readonly Dictionary<string, ForceFieldSpec> Specs = new()
        {
            // Pump: game Pump.FlowLength blown along the mouth (display angle - 90).
            ["pump"] = new ForceFieldSpec(Reach: 624, DirectionOffset: -90),
        };

        /// <summary>The force spec for <paramref name="element"/>, or null when it emits none.</summary>
        public static ForceFieldSpec? For(string element)
        {
            return Specs.TryGetValue(element, out ForceFieldSpec? spec) ? spec : null;
        }
    }
}
