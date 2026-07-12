using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Exact SteamTube physical and input geometry ported from the game.</summary>
    public static class SteamTubeGeometry
    {
        /// <summary>ITransporterItem collision radius around the tube body, in raw game units.</summary>
        public const double BodyCollisionRadius = 52.5;

        /// <summary>Valve input radius, kept only to make clear that it is not the body indicator.</summary>
        public const double ValveTouchRadius = 40;

        /// <summary>Valve input center offset along local +Y, in raw game units.</summary>
        public const double ValveTouchOffset = 28;

        /// <summary>Tube atlas source height; anchor 10 places its top-center at the object origin.</summary>
        public const double BodySourceHeight = 253;

        /// <summary>Valve center offset after the game's heightScale/mapScale cancellation.</summary>
        public const double ValveDrawOffset = 27;

        /// <summary>Maximum-state puff endpoint after the game's heightScale/mapScale cancellation.</summary>
        public const double MaximumSteamHeight = 141;

        /// <summary>Center offset of the top-anchored tube art in level space.</summary>
        public static double BodyDrawCenterOffset(double mapScale = SpritePlacement.MapScale)
        {
            return BodySourceHeight / (2.0 * mapScale);
        }

        /// <summary>Returns the body's circular collision bounds in editor level space.</summary>
        public static LevelBounds BodyBounds(
            double x,
            double y,
            double mapScale = SpritePlacement.MapScale)
        {
            double radius = BodyCollisionRadius / mapScale;
            return new LevelBounds(x - radius, y - radius, radius * 2, radius * 2);
        }
    }
}
