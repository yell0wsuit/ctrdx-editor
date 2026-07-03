using System.Collections.Generic;
using System.Text.Json;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Atlas
{
    /// <summary>Parses the TexturePacker JSON-array format used under content/.</summary>
    public static class AtlasJsonLoader
    {
        /// <summary>Parses atlas frames from a TexturePacker JSON document.</summary>
        public static IReadOnlyList<AtlasFrame> ParseFrames(string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("frames", out JsonElement framesEl))
            {
                return [];
            }

            List<AtlasFrame> frames = [];
            foreach (JsonElement fe in framesEl.EnumerateArray())
            {
                frames.Add(new AtlasFrame(
                    Filename: fe.GetProperty("filename").GetString() ?? string.Empty,
                    Frame: ReadRect(fe.GetProperty("frame")),
                    SpriteSource: ReadRect(fe.GetProperty("spriteSourceSize")),
                    SourceSize: ReadSize(fe.GetProperty("sourceSize")),
                    Rotated: fe.TryGetProperty("rotated", out JsonElement r) && r.GetBoolean(),
                    Trimmed: fe.TryGetProperty("trimmed", out JsonElement t) && t.GetBoolean()));
            }

            return frames;
        }

        private static IntRect ReadRect(JsonElement e)
        {
            return new(
            e.GetProperty("x").GetInt32(),
            e.GetProperty("y").GetInt32(),
            e.GetProperty("w").GetInt32(),
            e.GetProperty("h").GetInt32());
        }

        private static IntSize ReadSize(JsonElement e)
        {
            return new(
            e.GetProperty("w").GetInt32(),
            e.GetProperty("h").GetInt32());
        }
    }
}
