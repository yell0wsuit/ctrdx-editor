using System.Collections.Generic;

using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.Content
{
    /// <summary>
    /// One atlas frame layer of a composited object sprite, drawn back-to-front. The frame is resolved
    /// by <paramref name="Quad"/>, the zero-based position in the atlas JSON array used by the game.
    /// </summary>
    /// <param name="AtlasJsonRelPath">Path to the atlas JSON, relative to the content root.</param>
    /// <param name="AtlasImageBasePath">The atlas image path without its extension, so either platform's format resolves.</param>
    /// <param name="Quad">The layer's zero-based index in the atlas JSON array.</param>
    /// <param name="Tint">
    /// Color to multiply the layer's art by, or null to draw it as authored. Reproduces the game's
    /// <c>Image.useFullColorTint</c>, whose vertex color multiplies through the texture, and is how a
    /// grayscale mask frame (the magic hat's band) keeps its shading while taking a color the art does
    /// not contain. <see cref="SpriteCache"/> applies it once at load rather than at draw time.
    /// </param>
    /// <param name="Optional">
    /// Whether the sprite is still worth drawing without this layer. An optional layer's atlas is left
    /// out of <see cref="VisualDescriptorMap.RequiredFiles"/> and never makes its element unavailable, so
    /// a bundle predating the art loses only the detail the layer adds - the magic hat keeps its baked
    /// band rather than vanishing, and the rest of the bundle stays valid.
    /// </param>
    public sealed record SpriteLayer(
        string AtlasJsonRelPath,
        string AtlasImageBasePath,
        int Quad,
        RopeRgba? Tint = null,
        bool Optional = false);

    /// <summary>
    /// Maps an object element to the ordered atlas layers that make up its sprite, plus the per-object
    /// visual scale the game applies. Many CTR objects are assembled from several frames (candy wrapper,
    /// hook + ring, Om Nom + support cup); each <see cref="SpriteLayer"/> is centered on the object's
    /// (x,y) and aligned by its own TexturePacker trim within the shared sourceSize.
    /// <see cref="RandomBackLayers"/> are decorative variants: one is chosen per placed instance and
    /// drawn behind <see cref="Layers"/> (the bubble's random attached outline in the game's
    /// LoadBubble); they never affect selection bounds, hitboxes, or palette thumbnails.
    /// </summary>
    /// <param name="Element">The object's XML element name.</param>
    /// <param name="Layers">The atlas layers, drawn back-to-front.</param>
    /// <param name="Scale">The per-object visual scale the game applies.</param>
    /// <param name="RandomBackLayers">Decorative variants, one picked per placed instance; null means none.</param>
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
