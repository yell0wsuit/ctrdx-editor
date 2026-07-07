using System.Collections.Generic;

namespace CtrDxEditor.Content
{
    /// <summary>
    /// One atlas frame layer of a composited object sprite, drawn back-to-front. The frame is resolved
    /// by <paramref name="FrameName"/>, unless <paramref name="Quad"/> is set - then it is resolved by
    /// that zero-based position in the atlas (the engine's quad index), and the name is documentation
    /// only. Quad resolution is for atlases that share a frame order but not frame names (candy skins).
    /// </summary>
    public sealed record SpriteLayer(string AtlasJsonRelPath, string AtlasImageBasePath, string FrameName, int? Quad = null);

    /// <summary>
    /// Maps an object element to the ordered atlas layers that make up its sprite, plus the per-object
    /// visual scale the game applies. Many CTR objects are assembled from several frames (candy wrapper,
    /// hook + ring, Om Nom + support cup); each <see cref="SpriteLayer"/> is centered on the object's
    /// (x,y) and aligned by its own TexturePacker trim within the shared sourceSize.
    /// <see cref="RandomBackLayers"/> are decorative variants: one is chosen per placed instance and
    /// drawn behind <see cref="Layers"/> (the bubble's random attached outline in the game's
    /// LoadBubble); they never affect selection bounds, hitboxes, or palette thumbnails.
    /// </summary>
    public sealed record VisualDescriptor(
        string Element,
        IReadOnlyList<SpriteLayer> Layers,
        double Scale = 1.0,
        IReadOnlyList<SpriteLayer>? RandomBackLayers = null)
    {
        /// <summary>Decorative per-instance variants drawn behind <see cref="Layers"/>; empty when none.</summary>
        public IReadOnlyList<SpriteLayer> RandomBackLayers { get; init; } = RandomBackLayers ?? [];
    }
}
