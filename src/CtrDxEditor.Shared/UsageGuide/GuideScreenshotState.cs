namespace CtrDxEditor.UsageGuide
{
    /// <summary>Presentation state for a screenshot block with an optional embedded asset.</summary>
    public readonly record struct GuideScreenshotState(bool ShowImage, bool ShowPlaceholder)
    {
        /// <summary>Creates mutually exclusive image/placeholder visibility from an asset source.</summary>
        public static GuideScreenshotState From(string? source)
        {
            bool showImage = !string.IsNullOrWhiteSpace(source);
            return new GuideScreenshotState(showImage, !showImage);
        }
    }
}
