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
    /// </summary>
    public sealed record VisualDescriptor(
        string Element,
        IReadOnlyList<SpriteLayer> Layers,
        double Scale = 1.0);
}
