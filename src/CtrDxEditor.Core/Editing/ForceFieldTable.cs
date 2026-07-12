using System.Collections.Generic;
using System.Linq;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// A directional force emitter's field: how far it pushes and which way relative to the object's
    /// on-screen rotation. <see cref="Reach"/> is in game units (the same space as <see cref="HitboxDef"/>
    /// boxes), converted to level units by the sprite's <c>scale / mapScale</c> like <see cref="HitboxTable"/>.
    /// <see cref="DirectionOffset"/> is added to the object's display angle to get the push direction, so a
    /// pump that blows along its mouth (game impulse <c>rotate((0,-1), rotation)</c>) is <c>-90</c>.
    /// </summary>
    public sealed record ForceFieldSpec(
        double Reach,
        double DirectionOffset,
        IReadOnlyList<double>? Marks = null,
        double CoordinateScale = 1.0)
    {
        /// <summary>Distances from the emitter where the editor draws transverse level marks.</summary>
        public IReadOnlyList<double> LevelMarks { get; init; } = Marks ?? [];

        /// <summary>Converts the reach from its game coordinates into editor level units.</summary>
        public double LevelReach(double spriteScale, double mapScale = SpritePlacement.MapScale)
        {
            return Reach * CoordinateScale * spriteScale / mapScale;
        }

        /// <summary>Converts every authored level mark into editor level units.</summary>
        public IReadOnlyList<double> LevelMarkDistances(
            double spriteScale,
            double mapScale = SpritePlacement.MapScale)
        {
            return [.. LevelMarks.Select(mark => mark * CoordinateScale * spriteScale / mapScale)];
        }
    }

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
            // SteamTube.GetCurrentHeight: low 32.9, medium 94, high 141; force travels local -Y.
            ["steamTube"] = new ForceFieldSpec(
                Reach: 141,
                DirectionOffset: -90,
                Marks: [32.9, 94, 141],
                // GetCurrentHeight multiplies these authored heights by the level map scale in-game.
                CoordinateScale: SpritePlacement.MapScale),
        };

        /// <summary>The force spec for <paramref name="element"/>, or null when it emits none.</summary>
        public static ForceFieldSpec? For(string element)
        {
            return Specs.TryGetValue(element, out ForceFieldSpec? spec) ? spec : null;
        }
    }
}
