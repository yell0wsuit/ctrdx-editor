namespace CtrDxEditor.Core.Editing
{
    /// <summary>Which arrangement the editor shell uses for the current window width.</summary>
    public enum LayoutMode
    {
        /// <summary>Canvas full-bleed, panels behind a bottom tab bar and sheet. Phones and portrait tablets.</summary>
        Compact,

        /// <summary>The three-column palette / canvas / layers layout. Desktop and landscape tablets.</summary>
        Expanded,
    }

    /// <summary>Selects the editor layout from the available width.</summary>
    /// <remarks>
    /// The breakpoint is a width, not a platform check, so a narrowed desktop window exercises the compact
    /// shell — which makes it developable and testable without a device, and makes tablet split-view work
    /// without a special case.
    /// </remarks>
    public static class AdaptiveLayout
    {
        /// <summary>
        /// Width at and above which the three-column layout is used, following Tailwind's <c>lg</c>.
        /// </summary>
        /// <remarks>
        /// Chosen by arithmetic rather than taste: the fixed columns cost 200 + 280 + 2 = 482px, so at 768
        /// the canvas gets 286px (a sliver) while at 1024 it gets 542px and finally holds the majority.
        /// </remarks>
        public const double CompactMaxWidth = 1024;

        /// <summary>The layout mode for a given available width.</summary>
        /// <param name="width">Available width in logical pixels.</param>
        /// <returns>
        /// <see cref="LayoutMode.Expanded"/> at or above <see cref="CompactMaxWidth"/>; otherwise
        /// <see cref="LayoutMode.Compact"/>. Zero, negative and NaN widths are compact, because a control
        /// reports no bounds before its first layout pass and defaulting to expanded would flash the
        /// three-column layout on a phone at startup.
        /// </returns>
        public static LayoutMode ModeFor(double width)
        {
            return width >= CompactMaxWidth ? LayoutMode.Expanded : LayoutMode.Compact;
        }
    }
}
