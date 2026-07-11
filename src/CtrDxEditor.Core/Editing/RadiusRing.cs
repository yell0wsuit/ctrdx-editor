using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Resolves the resizable radius ring an object exposes: a grab's auto-catch <c>radius</c> or a light
    /// bulb's <c>litRadius</c>, both in level units. Returns null when the object has no ring or a
    /// non-positive radius. The circle geometry itself (edge hit-testing, drag mapping) lives in
    /// <see cref="GrabRadius"/>; this only maps an object to its radius value and the attribute that stores it,
    /// so one rendering + resize path covers both object types.
    /// </summary>
    public static class RadiusRing
    {
        /// <summary>The object's radius ring and the attribute holding it, or null when it has none.</summary>
        public static (double Radius, string Attr)? Of(LevelObject obj)
        {
            return obj.Type switch
            {
                "grab" => GrabRadius.Of(obj) is double r ? (r, "radius") : null,
                "lightBulb" => Lit(obj) is double lr ? (lr, "litRadius") : null,
                "rotatedCircle" => (VinylGeometry.DiscRadius(obj), "size"),
                _ => null,
            };
        }

        // The bulb's lit radius, or null when missing / non-positive (the game emits no glow then).
        private static double? Lit(LevelObject bulb)
        {
            return double.TryParse(
                       bulb.GetAttr("litRadius"), NumberStyles.Float, CultureInfo.InvariantCulture, out double r)
                   && r > 0
                ? r
                : null;
        }
    }
}
