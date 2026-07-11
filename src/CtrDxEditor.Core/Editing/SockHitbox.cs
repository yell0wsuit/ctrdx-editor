using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// The magic hat's game collision "mouth" box, ported from Sock.UpdateRotation
    /// (cuttherope-dx). In world units, relative to the hat center, the pre-rotation mouth
    /// spans X in [-90, +50] (width 140, shifted -20) and Y in [0, +15]. Level units are
    /// world units divided by MapScale, so the box is a fixed level size — independent of the
    /// hat's sprite scale and of the physics model. Rotation is applied by the overlay's
    /// existing DrawHitbox path (authored angle + 90 deg).
    /// </summary>
    public static class SockHitbox
    {
        /// <summary>
        /// Computes the mouth collision box in level units, centered on the hat at
        /// (<paramref name="x"/>, <paramref name="y"/>).
        /// </summary>
        /// <param name="x">Hat center X, in level units.</param>
        /// <param name="y">Hat center Y (mouth top edge), in level units.</param>
        /// <param name="mapScale">World-to-level scale factor; defaults to <see cref="SpritePlacement.MapScale"/>.</param>
        /// <returns>The pre-rotation mouth bounds in level units.</returns>
        public static LevelBounds Compute(double x, double y, double mapScale = SpritePlacement.MapScale)
        {
            double left = -90.0 / mapScale; // (x - sockWidth/2 - 20) world -> level
            double w = 140.0 / mapScale;    // sockWidth world -> level
            double h = 15.0 / mapScale;     // mouth depth world -> level
            return new LevelBounds(x + left, y, w, h);
        }
    }
}
