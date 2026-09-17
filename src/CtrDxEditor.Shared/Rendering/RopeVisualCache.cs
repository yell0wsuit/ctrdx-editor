using System.Runtime.CompilerServices;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Keeps each grab's built rope between frames. A pan or zoom redraws every rope without changing any of
    /// them, and building one samples and colors its whole curve, so a level of many ropes would otherwise
    /// rebuild them all on every frame.
    /// </summary>
    /// <remarks>
    /// An entry is reused only while every input <see cref="RopeRenderer.BuildRope(LevelObject, RopeTarget, RopePhysics, int)"/>
    /// reads is unchanged, so an edited grab, a moved target, or a new skin or physics model rebuilds its rope.
    /// Entries are keyed weakly on the grab's element and vanish with it.
    /// </remarks>
    internal sealed class RopeVisualCache
    {
        private readonly ConditionalWeakTable<XElement, Entry> _entries = [];

        /// <summary>The grab's rope visual, reused from an earlier frame when nothing it depends on has changed.</summary>
        /// <param name="grab">The authored grab.</param>
        /// <param name="rope">The grab's resolved target.</param>
        /// <param name="physics">The level's physics model.</param>
        /// <param name="skin">The active rope-skin index.</param>
        /// <returns>The rope visual, or null when the grab has nothing to hang from.</returns>
        public RopeVisual? Get(LevelObject grab, RopeTarget rope, RopePhysics physics, int skin)
        {
            if (rope.Target is not { } target)
            {
                return null;
            }

            // Mirrors what BuildRope reads: the grab's type, hook position and length, the target position, the
            // chain flag and (for a chain) its link seed, plus physics and skin.
            bool chain = ChainRope.IsChain(grab);
            Key key = new(
                grab.Type,
                grab.X,
                grab.Y,
                grab.GetAttr("length"),
                target.X,
                target.Y,
                chain,
                chain ? GrabRenderer.ChainSeed(grab) : 0,
                physics,
                skin);
            if (_entries.TryGetValue(grab.Element, out Entry? entry) && entry.Key == key)
            {
                return entry.Visual;
            }

            RopeVisual? visual = RopeRenderer.BuildRope(grab, rope, physics, skin);
            _entries.AddOrUpdate(grab.Element, new Entry(key, visual));
            return visual;
        }

        private readonly record struct Key(
            string Type,
            double X,
            double Y,
            string? Length,
            double TargetX,
            double TargetY,
            bool Chain,
            int ChainSeed,
            RopePhysics Physics,
            int Skin);

        private sealed record Entry(Key Key, RopeVisual? Visual);
    }
}
