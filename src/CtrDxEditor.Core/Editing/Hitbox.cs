using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>A center-relative collision rectangle in cuttherope-dx world units.</summary>
    public readonly record struct GameRect(double X, double Y, double W, double H);

    /// <summary>One object's desktop and mobile collision rectangles in game world space.</summary>
    public sealed record HitboxDef(
        string Element,
        GameRect Desktop,
        GameRect Phone,
        double DesktopTolerance = 0,
        double PhoneTolerance = 0);

    /// <summary>Which physics model's collision geometry to use.</summary>
    public enum HitboxModel
    {
        /// <summary>The desktop physics model.</summary>
        Desktop,

        /// <summary>The mobile/WP7 physics model transformed into desktop world units.</summary>
        Phone,
    }

    /// <summary>
    /// Maps the active cuttherope-dx collision geometry into editor level space. Definitions are stored
    /// center-relative in raw world units, exactly where the game performs collision checks; visual sprite
    /// scale is intentionally ignored because it does not transform <c>GameObject.bb</c> or collision strips.
    /// </summary>
    public static class HitboxTable
    {
        private static readonly Dictionary<string, HitboxDef> Defs =
            new HitboxDef[]
            {
                // Bounding boxes are converted from top-left texture offsets using BaseElement's integer
                // center anchor (width >> 1), then WP7 rows are scaled by ActivePhysicsConstants.Wp7ToWorldScale.
                // PointInRect capture square: 2 x BubbleCaptureRadius (85 desktop; 30 x 3 = 90 world mobile).
                new("bubble", new(-85, -85, 170, 170), new(-90, -90, 180, 180)),
                new("candy", new(-54, -52, 112, 104), new(-58, -62, 105, 105)),
                new("candyL", new(-41, -33, 88, 76), new(-40, -41, 69, 72)),
                new("candyR", new(-41, -33, 88, 76), new(-40, -41, 69, 72)),
                new("star", new(-48, -47, 82, 82), new(-52, -51, 90, 90)),
                new("target", new(-56, 30, 108, 2), new(-50, 10, 75, 3)),

                // ActivePhysicsConstants.SpikesCollisionLineWidth and SpikesCollisionBandHalfHeight,
                // inflated by the 15-unit spikeCollisionRadius candy tolerance (literal, both models).
                new("spike1", Centered(212, 10), Centered(204, 30), 15, 15),
                new("spike2", Centered(333, 10), Centered(318, 30), 15, 15),
                new("spike3", Centered(453, 10), Centered(438, 30), 15, 15),
                new("spike4", Centered(566, 10), Centered(543, 30), 15, 15),
                new("spike1_toggled", Centered(202, 10), Centered(204, 30), 15, 15),
                new("spike2_toggled", Centered(319, 10), Centered(354, 30), 15, 15),
                new("spike3_toggled", Centered(444, 10), Centered(426, 30), 15, 15),
                new("spike4_toggled", Centered(559, 10), Centered(534, 30), 15, 15),
                new("electro", Centered(433, 10), Centered(411, 30), 15, 15),

                // ActivePhysicsConstants.BouncerCollisionWidth and BouncerHeight, inflated by
                // BouncerCollisionRadius (40 desktop; 20 mobile x Wp7ToWorldScale(3) = 60 world).
                new("bouncer1", Centered(194, 10), Centered(138, 30), 40, 60),
                new("bouncer2", Centered(302, 10), Centered(273, 30), 40, 60),

                // ActivePhysicsConstants.RocketCatchBox*; the rocket's 0.7 scale affects artwork only.
                new(
                    "rocket",
                    Centered(214.8, 8.95, centerX: -21.5, centerY: -0.5),
                    Centered(208.8, 8.7, centerX: -25.5, centerY: 0)),
            }.ToDictionary(d => d.Element);

        /// <summary>Returns the active model selected by a level's <c>useMobilePhysics</c> setting.</summary>
        public static HitboxModel ModelFor(bool useMobilePhysics)
        {
            return useMobilePhysics ? HitboxModel.Phone : HitboxModel.Desktop;
        }

        /// <summary>
        /// Computes an object's level-space collision bounds, including state-dependent variants such as
        /// rotatable spike quads.
        /// </summary>
        public static LevelBounds? Compute(
            LevelObject obj,
            double scale,
            HitboxModel model,
            double mapScale = SpritePlacement.MapScale)
        {
            string element = SpikeObject.IsSpike(obj.Type) && SpikeObject.IsToggled(obj)
                ? $"{obj.Type}_toggled"
                : obj.Type;
            return Compute(element, obj.X, obj.Y, scale, model, mapScale);
        }

        /// <summary>
        /// Computes an element's level-space collision bounds at object center
        /// (<paramref name="x"/>, <paramref name="y"/>), or <see langword="null"/> when unsupported.
        /// </summary>
        public static LevelBounds? Compute(
            string element,
            double x,
            double y,
            double scale,
            HitboxModel model,
            double mapScale = SpritePlacement.MapScale)
        {
            _ = scale; // Collision geometry is authored in world space and does not inherit visual scale.

            if (element == "sock")
            {
                return SockHitbox.Compute(x, y, model, mapScale);
            }

            if (!Defs.TryGetValue(element, out HitboxDef? def))
            {
                return null;
            }

            GameRect box = model == HitboxModel.Phone ? def.Phone : def.Desktop;
            double tol = model == HitboxModel.Phone ? def.PhoneTolerance : def.DesktopTolerance;
            return new LevelBounds(
                x + ((box.X - tol) / mapScale),
                y + ((box.Y - tol) / mapScale),
                (box.W + (2 * tol)) / mapScale,
                (box.H + (2 * tol)) / mapScale);
        }

        private static GameRect Centered(
            double width,
            double height,
            double centerX = 0,
            double centerY = 0)
        {
            return new GameRect(
                centerX - (width / 2.0),
                centerY - (height / 2.0),
                width,
                height);
        }
    }
}
