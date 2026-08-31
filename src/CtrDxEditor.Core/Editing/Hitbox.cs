using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>A center-relative collision rectangle in cuttherope-dx world units.</summary>
    /// <param name="X">The left edge as an offset from the object's center, not an absolute coordinate; negative places it left of center.</param>
    /// <param name="Y">The top edge as an offset from the object's center; negative places it above center.</param>
    /// <param name="W">The width in world units, extending right from <paramref name="X"/>.</param>
    /// <param name="H">The height in world units, extending down from <paramref name="Y"/>.</param>
    public readonly record struct GameRect(double X, double Y, double W, double H);

    /// <summary>One object's desktop and mobile collision rectangles in game world space.</summary>
    /// <param name="Element">The XML element name this geometry applies to.</param>
    /// <param name="Desktop">The collision rectangle under the desktop physics model.</param>
    /// <param name="Phone">The collision rectangle under the mobile/WP7 physics model.</param>
    /// <param name="DesktopTolerance">Slack added to <paramref name="Desktop"/> on every side, in world units; 0 keeps the box tight.</param>
    /// <param name="PhoneTolerance">Slack added to <paramref name="Phone"/> on every side, in world units; 0 keeps the box tight.</param>
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
                // The mobile widths are normalized from the iOS high-resolution quads rather than
                // measured: (100 / 1.5) - 20 = 46.67 and (150 / 1.5) - 20 = 80 authored units, each
                // the quad width brought into authored coordinates less the 20-unit end cap, then
                // scaled to world.
                new("bouncer1", Centered(194, 10), Centered(140, 30), 40, 60),
                new("bouncer2", Centered(302, 10), Centered(240, 30), 40, 60),

                // ActivePhysicsConstants.RocketCatchBox*; the rocket's 0.7 scale affects artwork only.
                new(
                    "rocket",
                    Centered(214.8, 8.95, centerX: -21.5, centerY: -0.5),
                    Centered(208.8, 8.7, centerX: -25.5, centerY: 0)),

                // A Time Travel rocket catches over a wider slat: 0.65 of the 358-pixel quad rather
                // than 0.6, keeping the desktop height and centre offset. The flag is a mode of mobile
                // physics, so only the phone column is ever reached, but both are filled so the row
                // cannot read as empty if that ever changes.
                new(
                    "rocket_timetravel",
                    Centered(232.7, 8.95, centerX: -21.5, centerY: -0.5),
                    Centered(232.7, 8.95, centerX: -21.5, centerY: -0.5)),

                // GameScene.GetSnailBoundingBox. The desktop row is obj_snail frame_08_shell's trim
                // within its 393x418 canvas; Snail.InitWithTexture calls DoRestoreCutTransparency, so
                // the element keeps that untrimmed size and anchor 18 centers it (393>>1, 418>>1).
                new("load", new(-63, -38, 120, 138), new(-67, -44, 114, 132)),
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
        /// <param name="obj">The object to bound; its state selects variants such as a toggled spike's quad.</param>
        /// <param name="scale">Accepted for call-site symmetry with sprite placement but ignored: collision geometry is authored in world space and does not inherit visual scale.</param>
        /// <param name="model">The physics model, selecting desktop or phone collision geometry.</param>
        /// <param name="mapScale">Atlas pixels per level unit; defaults to the standard map scale.</param>
        /// <param name="timeTravelRockets">
        /// Whether the level asked for Time Travel's rocket tuning, which widens a rocket's catch box.
        /// Unlike a toggled spike this is a level setting rather than object state, so it cannot be read
        /// off <paramref name="obj"/> and arrives from the caller instead.
        /// </param>
        /// <returns>The collision bounds in level units, or null when the element has no hitbox.</returns>
        public static LevelBounds? Compute(
            LevelObject obj,
            double scale,
            HitboxModel model,
            double mapScale = SpritePlacement.MapScale,
            bool timeTravelRockets = false)
        {
            string element = obj.Type switch
            {
                _ when SpikeObject.IsSpike(obj.Type) && SpikeObject.IsToggled(obj) => $"{obj.Type}_toggled",
                "rocket" when timeTravelRockets => "rocket_timetravel",
                _ => obj.Type,
            };
            return Compute(element, obj.X, obj.Y, scale, model, mapScale);
        }

        /// <summary>
        /// Computes an element's level-space collision bounds at object center
        /// (<paramref name="x"/>, <paramref name="y"/>), or <see langword="null"/> when unsupported.
        /// </summary>
        /// <param name="element">The element name, including any state suffix such as <c>_toggled</c>.</param>
        /// <param name="x">The object's center X in level units.</param>
        /// <param name="y">The object's center Y in level units.</param>
        /// <param name="scale">Accepted for call-site symmetry with sprite placement but ignored: collision geometry is authored in world space and does not inherit visual scale.</param>
        /// <param name="model">The physics model, selecting desktop or phone collision geometry.</param>
        /// <param name="mapScale">Atlas pixels per level unit; defaults to the standard map scale.</param>
        /// <returns>The collision bounds in level units, or null when the element has no hitbox.</returns>
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
