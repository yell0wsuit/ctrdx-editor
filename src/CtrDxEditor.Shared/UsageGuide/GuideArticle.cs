using System.Collections.Generic;
using System;

using CtrDxEditor.Localization;

namespace CtrDxEditor.UsageGuide
{
    /// <summary>A searchable, navigable article in the built-in Usage Guide.</summary>
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
