namespace CtrDxEditor.UsageGuide
{
    /// <summary>Pure adaptive-layout policy for the Usage Guide shell.</summary>
    public static class UsageGuideLayout
    {
        /// <summary>Minimum width at which the table of contents can remain beside an article.</summary>
        public const double SidebarMinWidth = 900;

        /// <summary>Whether the current viewport should keep the table of contents persistently visible.</summary>
        public static bool UsesPersistentSidebar(double width, double height)
        {
            return width >= SidebarMinWidth && width >= height;
        }
    }
}
