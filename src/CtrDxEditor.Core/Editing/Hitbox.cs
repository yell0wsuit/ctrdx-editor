using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>A raw game bounding box (offset + size) in game texture pixels.</summary>
    public readonly record struct GameRect(double X, double Y, double W, double H);

    /// <summary>
    /// One object's ported hitbox: the verbatim game bounding boxes plus the reference frame
    /// (the object width/height the box was authored against, where drawX = x - RefWidth/2).
    /// </summary>
    public sealed record HitboxDef(
        string Element,
        GameRect Desktop,
        GameRect Phone,
        double RefWidth,
        double RefHeight);

    /// <summary>Which physics model's bounding box to use.</summary>
    public enum HitboxModel
    {
        /// <summary>The desktop physics bounding box.</summary>
        Desktop,

        /// <summary>The phone physics bounding box.</summary>
        Phone,
    }

    /// <summary>
    /// Maps ported game collision boxes (GameScene.BoundingBoxes.cs) into editor level space,
    /// mirroring <see cref="SpritePlacement"/>. Because a box is a sub-region of the same sprite
    /// scaled by the same s = scale / mapScale, it stays glued to the drawn art at any zoom.
    /// Add an object by adding a row to <see cref="Defs"/>.
    /// </summary>
    public static class HitboxTable
    {
        /// <summary>ActivePhysicsConstants.Wp7ToWorldScale in the main project.</summary>
        public const double Wp7ToWorldScale = 3.0;

        //                                 desktop bb            phone bb (pre-scale)  ref frame
        private static readonly Dictionary<string, HitboxDef> Defs =
            new HitboxDef[]
            {
                new("bubble", new(48, 48, 152, 152), new(0, 0, 57, 57), 250, 250),
                new("candy", new(142, 157, 112, 104), new(46, 49, 35, 35), 393, 418),
                new("star", new(70, 64, 82, 82), new(22, 20, 30, 30), 236, 223),
                new("target", new(264, 350, 108, 2), new(90, 110, 25, 1), 640, 640),
                new("pump", new(300, 300, 175, 175), new(94, 95, 57, 57), 761, 761),
            }.ToDictionary(d => d.Element);

        /// <summary>
        /// The level-space box for <paramref name="element"/> at object center
        /// (<paramref name="x"/>,<paramref name="y"/>), or <see langword="null"/> if the element
        /// has no ported hitbox.
        /// </summary>
        public static LevelBounds? Compute(
            string element,
            double x,
            double y,
            double scale,
            HitboxModel model,
            double mapScale = SpritePlacement.MapScale)
        {
            if (!Defs.TryGetValue(element, out HitboxDef? def))
            {
                return null;
            }

            GameRect bb = model == HitboxModel.Phone
                ? new GameRect(
                    def.Phone.X * Wp7ToWorldScale, def.Phone.Y * Wp7ToWorldScale,
                    def.Phone.W * Wp7ToWorldScale, def.Phone.H * Wp7ToWorldScale)
                : def.Desktop;

            double s = scale / mapScale;
            double cx = x + ((bb.X + (bb.W / 2) - (def.RefWidth / 2)) * s);
            double cy = y + ((bb.Y + (bb.H / 2) - (def.RefHeight / 2)) * s);
            double w = bb.W * s;
            double h = bb.H * s;
            return new LevelBounds(cx - (w / 2), cy - (h / 2), w, h);
        }
    }
}
