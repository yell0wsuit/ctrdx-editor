namespace CtrDxEditor.UsageGuide
{
    /// <summary>Pure adaptive-layout policy for the Usage Guide shell.</summary>
    public static class UsageGuideLayout
    {
        /// <summary>Minimum width at which the table of contents can remain beside an article.</summary>
        public const double SidebarMinWidth = 900;

        /// <summary>Minimum width at which navigation and search can share one toolbar row.</summary>
        public const double ToolbarMinWidth = 760;

        /// <summary>Whether the current viewport should keep the table of contents persistently visible.</summary>
        /// <param name="width">Available guide width in logical pixels.</param>
        /// <param name="height">Available guide height in logical pixels.</param>
        /// <returns>
        /// <see langword="true"/> for a sufficiently wide landscape viewport; otherwise
        /// <see langword="false"/> so the table of contents uses a drawer.
        /// </returns>
        public static bool UsesPersistentSidebar(double width, double height)
        {
            return width >= SidebarMinWidth && width >= height;
        }

        /// <summary>Whether search should move beneath navigation for the current guide width.</summary>
        /// <param name="width">Available guide width in logical pixels.</param>
        /// <returns>
        /// <see langword="true"/> when the toolbar needs two rows to keep search comfortably sized;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool UsesStackedToolbar(double width)
        {
            return width < ToolbarMinWidth;
        }
    }
}
