using System;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// The resizable circle a DX orbit path draws, and what a drag on its edge writes back. The orbit twin
    /// of <see cref="RadiusRing"/>: that one maps an object to a stored radius attribute, this one to the
    /// radius encoded in a <c>RC</c>/<c>RW</c> path. Circle geometry (edge hit-testing, distance) is shared
    /// with <see cref="GrabRadius"/>, so both rings feel the same under the pointer.
    /// </summary>
    public static class OrbitRing
    {
        /// <summary>
        /// Smallest radius a drag can produce. Below this a circular path tessellates to fewer than two
        /// points, <see cref="MoverPath.HasActiveMovement"/> goes false, and the ring under the pointer
        /// would vanish mid-drag.
        /// </summary>
        public const int Min = 4;

        /// <summary>The object's orbit radius and travel direction, or null when it has no ring to resize.</summary>
        /// <param name="obj">Object whose <c>path</c> and <c>moveSpeed</c> attributes are inspected.</param>
        /// <returns>
        /// The radius in level units and whether it travels clockwise, or null when the object is not
        /// orbiting — the same condition under which no ring is drawn.
        /// </returns>
        public static (double Radius, bool Clockwise)? Of(LevelObject obj)
        {
            return ObjectSpin.IsOrbital(obj)
                ? (ObjectSpin.OrbitRadius(obj), ObjectSpin.OrbitClockwise(obj))
                : null;
        }

        /// <summary>New orbit radius from a drag point, rounded whole and clamped to <see cref="Min"/>.</summary>
        /// <param name="center">The orbiting object's authored position, which is the circle's centre.</param>
        /// <param name="point">The pointer position in level units.</param>
        /// <returns>The radius to write, in level units.</returns>
        public static int FromDrag(Vec2 center, Vec2 point)
        {
            return Math.Max(Min, (int)Math.Round(GrabRadius.Distance(center, point)));
        }

        /// <summary>Rewrites the orbit's radius, keeping its direction and speed.</summary>
        /// <param name="obj">The orbiting object to resize.</param>
        /// <param name="radius">The new radius in level units; values below <see cref="Min"/> are clamped.</param>
        public static void Apply(LevelObject obj, int radius)
        {
            ObjectSpin.SetOrbital(obj, enabled: true, Math.Max(Min, radius), ObjectSpin.OrbitClockwise(obj));
        }
    }
}
