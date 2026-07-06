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
        private const string GunJson = "images/obj_gun.json";
        private const string GunImageBase = "images/obj_gun";
        private const string SpiderJson = "images/obj_spider.json";
        private const string SpiderImageBase = "images/obj_spider";
        private const string StickerJson = "images/obj_sticker.json";
        private const string StickerImageBase = "images/obj_sticker";
        private const string BubbleJson = "images/obj_bubble.json";
        private const string BubbleImageBase = "images/obj_bubble";
        private const string LighterJson = "images/obj_lighter.json";
        private const string LighterImageBase = "images/obj_lighter";

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

            // Split candy halves use the part frames from the same candy atlas at the same 0.71 scale.
            new("candyL",
            [
                new SpriteLayer(CandyJson, CandyImageBase, "frame_08_part_1.png"),
            ], Scale: 0.71),
            new("candyR",
            [
                new SpriteLayer(CandyJson, CandyImageBase, "frame_09_part_2.png"),
            ], Scale: 0.71),

            // Grab hook = arm + ring (share sourceSize 276x276).
            new("grab",
            [
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_01_frame_0000.png"),
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_01_frame_0001.png"),
            ]),

            // Wheel grab = regulated hook wheel art. The game draws the base wheel first, then the
            // variable/idle wheel face in front; the editor uses the idle face for a stable preview.
            new("grab_wheel",
            [
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_regulated_frame_0000.png"),
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_regulated_frame_0001.png"),
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_regulated_frame_0003.png"),
            ]),

            // Gun grab = back, aim arrow, and front cap from the gun atlas.
            new("grab_gun",
            [
                new SpriteLayer(GunJson, GunImageBase, "frame_00_GunBackQuad.png"),
                new SpriteLayer(GunJson, GunImageBase, "frame_01_GunArrowQuad.png"),
                new SpriteLayer(GunJson, GunImageBase, "frame_02_GunFrontQuad.png"),
            ]),

            // Spider grab = a static frame from the spider idle loop. In-game the spider animates along
            // the rope; the editor keeps it centered on the authored grab point as a state marker.
            new("grab_spider",
            [
                new SpriteLayer(SpiderJson, SpiderImageBase, "frame_0000.png"),
            ]),

            // Suction cup grab = sticker cup. Kicked=false uses quads 3/4; kicked=true uses
            // quads 1/2 after updateKickState detaches the cup.
            new("grab_suction",
            [
                new SpriteLayer(StickerJson, StickerImageBase, "frame_0003.png"),
                new SpriteLayer(StickerJson, StickerImageBase, "frame_0004.png"),
            ]),
            new("grab_suction_kicked",
            [
                new SpriteLayer(StickerJson, StickerImageBase, "frame_0001.png"),
                new SpriteLayer(StickerJson, StickerImageBase, "frame_0002.png"),
            ]),

            // Auto-catch grab = the auto-hook art (game HookAuto quads 4/5, back + front), used in place
            // of the fixed hook when radius is positive. Not a placeable element; picked by grab state.
            new("grab_auto",
            [
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_auto_frame_0000.png"),
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_auto_frame_0001.png"),
            ]),

            // Movable-rail pieces (game HookMovable quads 6/8/7 = left cap, center tile, right cap). The
            // canvas assembles these into a rail of arbitrary length; they are never drawn centered like a
            // normal sprite, so the layer order here is left, center, right for the renderer to index.
            new("grab_rail",
            [
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_movable_frame_0000.png"),
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_movable_frame_0002.png"),
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_movable_frame_0001.png"),
            ]),

            // Movable-rail hook (game HookMovable quad 10), drawn at the hook rest point in place of the
            // fixed hook when moveLength > 0. Not placeable; picked by grab state.
            new("grab_movable",
            [
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_movable_frame_0004.png"),
            ]),

            // Highlighted movable-rail hook (game HookMovable quad 9), shown in place of grab_movable while
            // the mover is being dragged (game moverDragging != -1). Not placeable; picked by grab state.
            new("grab_movable_highlight",
            [
                new SpriteLayer(HookJson, HookImageBase, "obj_hook_movable_frame_0003.png"),
            ]),

            // Bubble = attached quad 0 over one random attached outline, matching LoadBubble's
            // parent quad RND_RANGE(1,3) plus child Image quad 0.
            new("bubble",
            [
                new SpriteLayer(BubbleJson, BubbleImageBase, "obj_bubble_attached_frame_0000.png"),
            ],
            RandomBackLayers:
            [
                new SpriteLayer(BubbleJson, BubbleImageBase, "obj_bubble_attached_frame_0001.png"),
                new SpriteLayer(BubbleJson, BubbleImageBase, "obj_bubble_attached_frame_0002.png"),
                new SpriteLayer(BubbleJson, BubbleImageBase, "obj_bubble_attached_frame_0003.png"),
            ]),

            // Star = glow halo (frame 0, the ImgObjStarIdleGlow quad) behind the star body. The body
            // idle loops frames 1-18 in game; frame 18 is the fullest front-on pose for a static view.
            new("star",
            [
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle", "frame_0000.png"),
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle", "frame_0018.png"),
            ]),

            // Gravity button.
            new("gravitySwitch",
            [
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle", "frame_0056.png"),
            ]),

            // Light bulb = lantern bottle + top (the lit glow quad is omitted for a static thumbnail).
            new("lightBulb",
            [
                new SpriteLayer(LighterJson, LighterImageBase, "02_bottle.png"),
                new SpriteLayer(LighterJson, LighterImageBase, "03_top.png"),
            ]),

            // Lit-glow halo quad, drawn additively under the bottle at 1.5x litRadius by GlowDrawOperation.
            // A separate element so it is never composited into the static bulb thumbnail or selection bounds.
            new("lightBulb_glow",
            [
                new SpriteLayer(LighterJson, LighterImageBase, "01_light.png"),
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
                .. All.SelectMany(v => v.Layers.Concat(v.RandomBackLayers))
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
