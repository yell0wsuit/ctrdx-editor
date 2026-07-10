namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Vertical placement of the magic hat's sprite, ported from LoadSocks + GameScene.Draw
    /// (cuttherope-dx). The game top-anchors the untrimmed hat sprite, shifts the draw up 85 world-units
    /// (<c>sock.y -= 85</c>), and — via <c>rotationCenterY</c> — scales and rotates it about the object
    /// anchor, so the anchor ends up 85 world-units below the sprite's top edge. The editor instead
    /// center-anchors the untrimmed sprite (anchor at sourceHeight/2 below the top, see
    /// <see cref="SpritePlacement"/>), which draws the hat too high. Shift it down by the difference.
    /// The result is independent of the sprite's trim: the trim offset cancels between the two anchorings.
    /// </summary>
    public static class SockSprite
    {
        /// <summary>Game constant: the hat's object anchor sits this many world-units below the sprite top.</summary>
        public const double AnchorFromTop = 85.0;

        /// <summary>
        /// The downward level-space offset that moves the editor's center-anchored hat sprite onto the
        /// game's anchor point, so the drawn hat lines up with its (game-accurate) mouth hitbox.
        /// </summary>
        /// <param name="sourceHeight">The sprite's untrimmed source height in atlas pixels.</param>
        /// <param name="scale">The per-object sprite scale (the hat uses 0.7, matching the game).</param>
        /// <param name="mapScale">World-to-level scale (<see cref="SpritePlacement.MapScale"/>).</param>
        public static double DrawOffsetY(double sourceHeight, double scale, double mapScale = SpritePlacement.MapScale)
        {
            return ((sourceHeight / 2.0) - AnchorFromTop) * scale / mapScale;
        }
    }
}
