namespace CtrDxEditor.UsageGuide
{
    /// <summary>Presentation state for a screenshot block with an optional embedded asset.</summary>
    /// <param name="ShowImage">Whether the supplied screenshot image should be visible.</param>
    /// <param name="ShowPlaceholder">Whether the named placeholder should be visible.</param>
    public readonly record struct GuideScreenshotState(bool ShowImage, bool ShowPlaceholder)
    {
        /// <summary>Creates mutually exclusive image/placeholder visibility from an asset source.</summary>
        /// <param name="source">Optional Avalonia resource URI supplied by the screenshot block.</param>
        /// <returns>Visibility flags that show either the image or its placeholder.</returns>
        public static GuideScreenshotState From(string? source)
        {
            bool showImage = !string.IsNullOrWhiteSpace(source);
            return new GuideScreenshotState(showImage, !showImage);
        }
    }
}
