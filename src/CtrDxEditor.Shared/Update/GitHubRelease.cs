using System.Text.Json.Serialization;

namespace CtrDxEditor.Update
{
    /// <summary>The single field the update check reads from a GitHub release payload.</summary>
    /// <remarks>
    /// The response carries dozens of other properties; leaving them out is deliberate, since
    /// <see cref="System.Text.Json"/> ignores unmapped members and every extra one would be dead
    /// weight in the source-generated context.
    /// </remarks>
    public sealed class GitHubRelease
    {
        /// <summary>Git tag the release was published from, e.g. <c>v1.0.1</c>.</summary>
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
    }
}
