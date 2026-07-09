namespace CtrDxEditor.ViewModels
{
    /// <summary>Which live object-animation preview is currently active.</summary>
    public enum AnimationPreviewMode
    {
        /// <summary>No live preview; objects render at their authored static state.</summary>
        Off,

        /// <summary>Every eligible animated object renders with live elapsed motion.</summary>
        All,

        /// <summary>Only <see cref="EditorViewModel.AnimationPreviewObject"/> renders with live elapsed motion.</summary>
        Focused,
    }
}
