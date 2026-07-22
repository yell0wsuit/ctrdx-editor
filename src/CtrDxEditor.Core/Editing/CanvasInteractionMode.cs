namespace CtrDxEditor.Core.Editing
{
    /// <summary>What a single primary pointer contact on the canvas does.</summary>
    /// <remarks>
    /// Touch has no middle-drag and no modifier chord, so the two gestures a phone needs — edit the level
    /// and move the view — cannot be told apart by which button is held and have to be separated by an
    /// explicit mode instead. Mouse and pen keep every gesture they already had; the mode only gates the
    /// primary contact, and the compact rail is the only thing that ever sets it.
    /// </remarks>
    public enum CanvasInteractionMode
    {
        /// <summary>Presses select, drags move objects, and empty space pans. The existing behaviour.</summary>
        Edit,

        /// <summary>Every primary press pans the view, and the selection is left untouched.</summary>
        Pan,
    }
}
