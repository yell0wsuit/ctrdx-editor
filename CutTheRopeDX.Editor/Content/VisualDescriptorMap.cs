using System.Collections.Generic;
using System.Linq;

namespace CutTheRopeDX.Editor.Content
{
    public static class VisualDescriptorMap
    {
        private const string CandyJson = "images/candies/obj_candy_01_new.json";
        private const string CandyPng = "images/candies/obj_candy_01_new.png";
        private const string HookJson = "images/obj_hook.json";
        private const string HookPng = "images/obj_hook.png";

        private static readonly VisualDescriptor[] All =
        [
            // Om Nom = support cup (back) + character (front). Both atlases share a 640x640 canvas,
            // so the cup naturally sits below the mouth point with no manual offset.
            new("target",
            [
                new SpriteLayer("images/char_supports.json", "images/char_supports.png", "frame_0000.png"),
                new SpriteLayer("images/char_animations.json", "images/char_animations.png", "frame_0000.png"),
            ]),

            // Candy = wrapper bottom + body + wrapper top (all share sourceSize 393x418).
            new("candy",
            [
                new SpriteLayer(CandyJson, CandyPng, "frame_00_bottom.png"),
                new SpriteLayer(CandyJson, CandyPng, "frame_01_main.png"),
                new SpriteLayer(CandyJson, CandyPng, "frame_02_top.png"),
            ], Scale: 0.71),

            // Grab hook = arm + ring (share sourceSize 276x276).
            new("grab",
            [
                new SpriteLayer(HookJson, HookPng, "obj_hook_01_frame_0000.png"),
                new SpriteLayer(HookJson, HookPng, "obj_hook_01_frame_0001.png"),
            ]),

            // Star = glow halo (frame 0, the ImgObjStarIdleGlow quad) behind the star body. The body
            // idle loops frames 1-18 in game; frame 18 is the fullest front-on pose for a static view.
            new("star",
            [
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle.png", "frame_0000.png"),
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle.png", "frame_0018.png"),
            ]),
        ];

        public static IReadOnlyDictionary<string, VisualDescriptor> ByElement { get; } =
            All.ToDictionary(v => v.Element);

        public static VisualDescriptor? For(string element)
        {
            return ByElement.TryGetValue(element, out VisualDescriptor? v) ? v : null;
        }
    }
}
