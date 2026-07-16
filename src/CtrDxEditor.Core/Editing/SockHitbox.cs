using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// The magic hat's game collision "mouth" box, ported from Sock.UpdateRotation
    /// (cuttherope-dx). In world units, relative to the hat center, the pre-rotation mouth
    /// spans X in [-90, +50] and Y in [0, +15] for desktop physics. Mobile physics uses the
    /// WP7 mouth transformed into world units: X in [-45, +45], Y in [0, +3]. Level units are
    /// world units divided by MapScale, and neither model inherits the hat's visual sprite scale.
    /// </summary>
    public static class SockHitbox
    {
        /// <summary>
        /// Computes the mouth collision box in level units, centered on the hat at
        /// (<paramref name="x"/>, <paramref name="y"/>).
        /// </summary>
        /// <param name="x">Hat center X, in level units.</param>
        /// <param name="y">Hat center Y (mouth top edge), in level units.</param>
        /// <param name="model">The active desktop or mobile physics model.</param>
        /// <param name="mapScale">World-to-level scale factor; defaults to <see cref="SpritePlacement.MapScale"/>.</param>
        /// <returns>The pre-rotation mouth bounds in level units.</returns>
        public static LevelBounds Compute(
            double x,
            double y,
            HitboxModel model = HitboxModel.Desktop,
            double mapScale = SpritePlacement.MapScale)
        {
            double left = (model == HitboxModel.Phone ? -45.0 : -90.0) / mapScale;
            double w = (model == HitboxModel.Phone ? 90.0 : 140.0) / mapScale;
            double h = (model == HitboxModel.Phone ? 3.0 : 15.0) / mapScale;
            return new LevelBounds(x + left, y, w, h);
        }
    }
}
