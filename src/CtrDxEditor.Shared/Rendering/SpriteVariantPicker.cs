using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Chooses which decorative variant layer a placed object draws (the bubble's random attached
    /// outline). Mirrors the game's LoadBubble: rolled once per instance at random, then stable for
    /// the object's lifetime — so repaints and drags never flicker, and a reload re-rolls like the
    /// game does. Keyed on the object's underlying XElement, which survives the transient
    /// LevelObject wrappers each document read creates.
    /// </summary>
    public static class SpriteVariantPicker
    {
        private static readonly ConditionalWeakTable<XElement, StrongBox<int>> Rolls = [];

        /// <summary>A stable index in [0, <paramref name="count"/>) for <paramref name="element"/>.</summary>
        public static int Pick(XElement element, int count)
        {
            StrongBox<int> roll = Rolls.GetValue(element, _ => new StrongBox<int>(Random.Shared.Next()));
            return roll.Value % count;
        }
    }
}
