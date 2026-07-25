using System;
using System.Collections.Generic;

using CtrDxEditor.Localization;

namespace CtrDxEditor.UsageGuide
{
    /// <summary>Base type for structured Usage Guide article content.</summary>
    public abstract record GuideBlock;

    /// <summary>Ordinary explanatory copy.</summary>
    /// <param name="TextKey">Localization key for the paragraph text.</param>
    public sealed record GuideParagraph(string TextKey) : GuideBlock
    {
        /// <summary>Localized paragraph text.</summary>
        public string Text => Localizer.Get(TextKey);
    }

    /// <summary>A heading within an article.</summary>
    /// <param name="TextKey">Localization key for the heading text.</param>
    public sealed record GuideHeading(string TextKey) : GuideBlock
    {
        /// <summary>Localized heading text.</summary>
        public string Text => Localizer.Get(TextKey);
    }

    /// <summary>An ordered procedure.</summary>
    /// <param name="ItemsKey">Localization key for newline-separated procedure items.</param>
    public sealed record GuideSteps(string ItemsKey) : GuideBlock
    {
        /// <summary>Localized procedure items, separated by newlines in the translation catalog.</summary>
        public IReadOnlyList<string> Items =>
            Localizer.Get(ItemsKey).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>The visual importance of an article callout.</summary>
    public enum GuideCalloutKind
    {
        /// <summary>Optional advice that makes a workflow easier.</summary>
        Tip,

        /// <summary>Context the reader should know.</summary>
        Note,

        /// <summary>A condition that can produce a surprising or destructive result.</summary>
        Warning,
    }

    /// <summary>A visually distinct tip, note, or warning.</summary>
    /// <param name="Kind">Visual importance assigned to the callout.</param>
    /// <param name="TextKey">Localization key for the callout text.</param>
    public sealed record GuideCallout(GuideCalloutKind Kind, string TextKey) : GuideBlock
    {
        /// <summary>Localized callout text.</summary>
        public string Text => Localizer.Get(TextKey);

        /// <summary>Localized callout-kind label.</summary>
        public string Label => Localizer.Get($"Guide.Callout.{Kind}");

        /// <summary>Whether this callout should use the optional-advice palette.</summary>
        public bool IsTip => Kind == GuideCalloutKind.Tip;

        /// <summary>Whether this callout should use the informational palette.</summary>
        public bool IsNote => Kind == GuideCalloutKind.Note;

        /// <summary>Whether this callout should use the warning palette.</summary>
        public bool IsWarning => Kind == GuideCalloutKind.Warning;
    }

    /// <summary>One command and its key gesture.</summary>
    /// <param name="ActionKey">Localization key for the command name.</param>
    /// <param name="KeysKey">Localization key for the platform-neutral gesture notation.</param>
    public sealed record GuideShortcut(string ActionKey, string KeysKey)
    {
        /// <summary>Localized command name.</summary>
        public string Action => Localizer.Get(ActionKey);

        /// <summary>Localized, platform-neutral key notation.</summary>
        public string Keys => Localizer.Get(KeysKey);
    }

    /// <summary>A compact table of keyboard commands.</summary>
    /// <param name="Items">Command rows in display order.</param>
    public sealed record GuideShortcutTable(IReadOnlyList<GuideShortcut> Items) : GuideBlock;

    /// <summary>
    /// A replaceable illustration slot. An empty <see cref="Source"/> renders a placeholder labelled with
    /// <see cref="SuggestedFileName"/>.
    /// </summary>
    /// <param name="CaptionKey">Localization key for the illustration caption.</param>
    /// <param name="SuggestedFileName">Asset filename suggested to a future screenshot author.</param>
    /// <param name="Source">Optional Avalonia resource URI that replaces the placeholder.</param>
    public sealed record GuideScreenshot(
        string CaptionKey,
        string SuggestedFileName,
        string? Source = null) : GuideBlock
    {
        /// <summary>Localized illustration caption.</summary>
        public string Caption => Localizer.Get(CaptionKey);

        /// <summary>Whether an embedded screenshot source should be rendered.</summary>
        public bool ShowImage => GuideScreenshotState.From(Source).ShowImage;

        /// <summary>Whether the informative screenshot placeholder should be rendered.</summary>
        public bool ShowPlaceholder => GuideScreenshotState.From(Source).ShowPlaceholder;
    }
}
