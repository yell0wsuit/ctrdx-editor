using System;

namespace CtrDxEditor.Content
{
    /// <summary>
    /// Unsaved level work persisted so it survives a crash, a closed tab, or a Discard answer.
    /// </summary>
    /// <remarks>
    /// Decoration is carried because it is editor state rather than level XML, and the baseline is
    /// carried so a restored level still reads as modified and Review Changes still diffs against what
    /// was last saved.
    /// </remarks>
    public sealed record RecoverySnapshot
    {
        /// <summary>The live level, as a save would write it.</summary>
        public required string Xml { get; init; }

        /// <summary>The level as of its last load, new, or save.</summary>
        public required string BaselineXml { get; init; }

        /// <summary>The name of the file the level came from, for the Save As suggestion; null for new levels.</summary>
        public string? FileName { get; init; }

        /// <summary>The active rope skin id.</summary>
        public int RopeSkin { get; init; }

        /// <summary>The active background id.</summary>
        public int Background { get; init; }

        /// <summary>The active candy skin id.</summary>
        public int CandySkin { get; init; }

        /// <summary>The active Om Nom support id.</summary>
        public int OmNomSupport { get; init; }

        /// <summary>When the snapshot was captured.</summary>
        public DateTimeOffset SavedAt { get; init; }
    }
}
