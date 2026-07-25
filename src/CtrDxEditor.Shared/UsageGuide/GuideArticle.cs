using System;
using System.Collections.Generic;

using CtrDxEditor.Localization;

namespace CtrDxEditor.UsageGuide
{
    /// <summary>A searchable, navigable article in the built-in Usage Guide.</summary>
    /// <param name="Id">Stable identifier used by navigation history and related-topic links.</param>
    /// <param name="SectionKey">Localization key for the table-of-contents section.</param>
    /// <param name="TitleKey">Localization key for the article title.</param>
    /// <param name="SummaryKey">Localization key for the article summary.</param>
    /// <param name="SearchTermsKey">Localization key for vertical-bar-separated discovery aliases.</param>
    /// <param name="Blocks">Structured article content in reading order.</param>
    /// <param name="RelatedArticleIds">Stable identifiers offered as related topics.</param>
    public sealed record GuideArticle(
        string Id,
        string SectionKey,
        string TitleKey,
        string SummaryKey,
        string SearchTermsKey,
        IReadOnlyList<GuideBlock> Blocks,
        IReadOnlyList<string> RelatedArticleIds)
    {
        /// <summary>Localized table-of-contents group.</summary>
        public string Section => Localizer.Get(SectionKey);

        /// <summary>Localized article title.</summary>
        public string Title => Localizer.Get(TitleKey);

        /// <summary>Localized article summary.</summary>
        public string Summary => Localizer.Get(SummaryKey);

        /// <summary>Localized discovery aliases, separated by vertical bars in the translation catalog.</summary>
        public IReadOnlyList<string> SearchTerms =>
            Localizer.Get(SearchTermsKey).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
