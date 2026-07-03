using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CtrDxEditor.Content
{
    /// <summary>Source-generated JSON metadata for NativeAOT-safe app serialization.</summary>
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(EditorSettings))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    public sealed partial class AppJsonContext : JsonSerializerContext
    {
    }
}
