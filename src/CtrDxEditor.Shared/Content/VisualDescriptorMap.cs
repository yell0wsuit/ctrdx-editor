using System.Collections.Generic;
using System.Linq;

namespace CtrDxEditor.Content
{
    /// <summary>Built-in visual descriptor lookup for supported editor object sprites.</summary>
    public static class VisualDescriptorMap
    {
        private const string CandyJson = "images/candies/obj_candy_01_new.json";
        private const string CandyImageBase = "images/candies/obj_candy_01_new";
        private const string HookJson = "images/obj_hook.json";
        private const string HookImageBase = "images/obj_hook";

        private static readonly VisualDescriptor[] All =
        [
            // Om Nom = support cup (back) + character (front). Both atlases share a 640x640 canvas,
            // so the cup naturally sits below the mouth point with no manual offset.
            new("target",
            [
                new SpriteLayer("images/char_supports.json", "images/char_supports", "frame_0000.png"),
                new SpriteLayer("images/char_animations.json", "images/char_animations", "frame_0000.png"),
            ]),

            // Candy = wrapper bottom + body + wrapper top (all share sourceSize 393x418).
            new("candy",
            [
                new SpriteLayer(CandyJson, CandyImageBase, "frame_00_bottom.png"),
                new SpriteLayer(CandyJson, CandyImageBase, "frame_01_main.png"),
                new SpriteLayer(CandyJson, CandyImageBase, "frame_02_top.png"),
            ], Scale: 0.71),

            // Grab hook = arm + ring (share sourceSize 276x276).
            new("grab",
            [
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_01_frame_0000.png"),
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_01_frame_0001.png"),
            ]),

            // Star = glow halo (frame 0, the ImgObjStarIdleGlow quad) behind the star body. The body
            // idle loops frames 1-18 in game; frame 18 is the fullest front-on pose for a static view.
            new("star",
            [
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle", "frame_0000.png"),
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle", "frame_0018.png"),
            ]),
        ];

        /// <summary>All visual descriptors keyed by object element name.</summary>
        public static IReadOnlyDictionary<string, VisualDescriptor> ByElement { get; } =
            All.ToDictionary(v => v.Element);

        /// <summary>
        /// The distinct relative content paths every built-in sprite needs, for a given image extension
        /// (e.g. ".png" desktop, ".webp" browser): each layer's atlas image (base + extension) and its JSON.
        /// Used to reject a bundle that lacks the sprites this platform actually renders.
        /// </summary>
        public static IReadOnlyCollection<string> RequiredFiles(string imageExtension)
        {
            return
            [
                .. All.SelectMany(v => v.Layers)
                      .SelectMany(l => new[] { l.AtlasImageBasePath + imageExtension, l.AtlasJsonRelPath })
                      .Distinct(),
            ];
        }

        /// <summary>Returns a visual descriptor by object element name, or null when unsupported.</summary>
        public static VisualDescriptor? For(string element)
        {
            return ByElement.TryGetValue(element, out VisualDescriptor? v) ? v : null;
        }
    }
}
