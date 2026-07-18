namespace CtrDxEditor.Core.Document
{
    /// <summary>A stable structural coordinate for an object: its layer index and its index within that layer.</summary>
    /// <param name="LayerIndex">Zero-based index into <see cref="LevelDocument.Layers"/>.</param>
    /// <param name="IndexInLayer">Zero-based index into that layer's objects.</param>
    public readonly record struct ObjectRef(int LayerIndex, int IndexInLayer);
}
