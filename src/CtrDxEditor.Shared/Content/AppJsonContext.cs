using System.Collections.Generic;
using System.Text.Json.Serialization;

using CtrDxEditor.Update;

namespace CtrDxEditor.Content
{
    /// <summary>Source-generated JSON metadata for NativeAOT-safe app serialization.</summary>
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(EditorSettings))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(GitHubRelease))]
    public sealed partial class AppJsonContext : JsonSerializerContext
    {
    }
}
