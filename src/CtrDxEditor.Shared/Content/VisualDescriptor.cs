using System.Collections.Generic;

namespace CtrDxEditor.Content
{
    /// <summary>One atlas frame layer of a composited object sprite, drawn back-to-front.</summary>
    public sealed record SpriteLayer(string AtlasJsonRelPath, string AtlasImageBasePath, string FrameName);

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
