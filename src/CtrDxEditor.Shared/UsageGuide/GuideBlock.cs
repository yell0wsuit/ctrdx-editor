using System.Collections.Generic;
using System;

using CtrDxEditor.Localization;

namespace CtrDxEditor.UsageGuide
{
    /// <summary>Base type for structured Usage Guide article content.</summary>
    public abstract record GuideBlock;

    /// <summary>Ordinary explanatory copy.</summary>
    public sealed record GuideParagraph(string TextKey) : GuideBlock
    {
        /// <summary>Localized paragraph text.</summary>
        public string Text => Localizer.Get(TextKey);
    }

    /// <summary>A heading within an article.</summary>
    public sealed record GuideHeading(string TextKey) : GuideBlock
    {
        /// <summary>Localized heading text.</summary>
        public string Text => Localizer.Get(TextKey);
    }

    /// <summary>An ordered procedure.</summary>
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
    public sealed record GuideCallout(GuideCalloutKind Kind, string TextKey) : GuideBlock
    {
        /// <summary>Localized callout text.</summary>
        public string Text => Localizer.Get(TextKey);
    }

    /// <summary>One command and its key gesture.</summary>
    public sealed record GuideShortcut(string ActionKey, string KeysKey)
    {
        /// <summary>Localized command name.</summary>
        public string Action => Localizer.Get(ActionKey);

        /// <summary>Localized, platform-neutral key notation.</summary>
        public string Keys => Localizer.Get(KeysKey);
    }

    /// <summary>A compact table of keyboard commands.</summary>
    public sealed record GuideShortcutTable(IReadOnlyList<GuideShortcut> Items) : GuideBlock;

    /// <summary>
    /// A replaceable illustration slot. An empty <see cref="Source"/> renders a placeholder labelled with
    /// <see cref="SuggestedFileName"/>.
    /// </summary>
    public sealed record GuideScreenshot(
        string CaptionKey,
        string SuggestedFileName,
        string? Source = null) : GuideBlock
    {
        /// <summary>Localized illustration caption.</summary>
        public string Caption => Localizer.Get(CaptionKey);
    }
}
