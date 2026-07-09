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
        private const string PumpJson = "images/obj_pump.json";
        private const string PumpImageBase = "images/obj_pump";
        private const string SpikesJson = "images/obj_spikes.json";
        private const string SpikesImageBase = "images/obj_spikes";
        private const string ElectrodesJson = "images/obj_electrodes.json";
        private const string ElectrodesImageBase = "images/obj_electrodes";

        private static readonly VisualDescriptor[] All =
        [
            // Om Nom = support cup (back) + character (front). Both atlases share a 640x640 canvas,
            // so the cup naturally sits below the mouth point with no manual offset.
            new("target",
            [
                new SpriteLayer("images/char_supports.json", "images/char_supports", 0),
                new SpriteLayer("images/char_animations.json", "images/char_animations", 0),
            ]),

            // Night Om Nom keeps the selected support cup, but uses DX's classic sleeping spritesheet
            // (OriginalTargetAnimationBackend), not the Flash/XML blink frames.
            new("target_sleeping",
            [
                new SpriteLayer("images/char_supports.json", "images/char_supports", 0),
                new SpriteLayer("images/char_animations_sleeping.json", "images/char_animations_sleeping", 6),
            ]),

            // Candy = wrapper bottom + body + wrapper top (all share sourceSize 393x418). Addressed by
            // quad index, not frame name: candy skins share this frame order but not their frame names,
            // and SpriteCache swaps in the active skin's atlas at resolve time. Matches the game's
            // GameObject_createWithResIDQuad(candyResource, 0/1/2).
            new("candy",
            [
                new SpriteLayer(CandyJson, CandyImageBase, 0),
                new SpriteLayer(CandyJson, CandyImageBase, 1),
                new SpriteLayer(CandyJson, CandyImageBase, 2),
            ], Scale: 0.71),

            // Split candy halves = quad 8 (part_1) / quad 9 (part_2) of the same candy atlas, at the
            // same 0.71 scale. Matches the game's candyL/candyR quad indices in GameScene.LoadMetadata.
            new("candyL",
            [
                new SpriteLayer(CandyJson, CandyImageBase, 8),
            ], Scale: 0.71),
            new("candyR",
            [
                new SpriteLayer(CandyJson, CandyImageBase, 9),
            ], Scale: 0.71),

            // Grab hook = arm + ring (share sourceSize 276x276). Two interchangeable pairs exist: the game's
            // RandomHookBaseQuad rolls base 0 or 2 per placed hook and draws back=base, front=base+1. This is
            // the Hook01 pair (quads 0/1); "grab_02" is the Hook02 pair (quads 2/3). GrabRenderer.RenderSpriteKey
            // picks one per instance; the palette thumbnail and ghost preview always use this default pair.
            new("grab",
            [
                new SpriteLayer(HookJson, HookImageBase, 0),
                new SpriteLayer(HookJson, HookImageBase, 1),
            ]),

            // Second fixed-hook pair (game Hook02 quads 2/3), the alternate RandomHookBaseQuad roll. Same
            // 276x276 art as "grab"; picked per placed instance by GrabRenderer.RenderSpriteKey.
            new("grab_02",
            [
                new SpriteLayer(HookJson, HookImageBase, 2),
                new SpriteLayer(HookJson, HookImageBase, 3),
            ]),

            // Wheel grab = regulated hook wheel art. The game draws the base wheel first, then the
            // variable/idle wheel face in front; the editor uses the idle face for a stable preview.
            new("grab_wheel",
            [
                new SpriteLayer(HookJson, HookImageBase, 11),
                new SpriteLayer(HookJson, HookImageBase, 12),
                new SpriteLayer(HookJson, HookImageBase, 14),
            ]),

            // Gun grab = back, aim arrow, and front cap from the gun atlas.
            new("grab_gun",
            [
                new SpriteLayer(GunJson, GunImageBase, 0),
                new SpriteLayer(GunJson, GunImageBase, 1),
                new SpriteLayer(GunJson, GunImageBase, 2),
            ]),

            // Spider grab = a static frame from the spider idle loop. In-game the spider animates along
            // the rope; the editor keeps it centered on the authored grab point as a state marker.
            new("grab_spider",
            [
                new SpriteLayer(SpiderJson, SpiderImageBase, 0),
            ]),

            // Suction cup grab = sticker cup. Kicked=false uses quads 3/4; kicked=true uses
            // quads 1/2 after updateKickState detaches the cup.
            new("grab_suction",
            [
                new SpriteLayer(StickerJson, StickerImageBase, 3),
                new SpriteLayer(StickerJson, StickerImageBase, 4),
            ]),
            new("grab_suction_kicked",
            [
                new SpriteLayer(StickerJson, StickerImageBase, 1),
                new SpriteLayer(StickerJson, StickerImageBase, 2),
            ]),

            // Auto-catch grab = the auto-hook art (game HookAuto quads 4/5, back + front), used in place
            // of the fixed hook when radius is positive. Not a placeable element; picked by grab state.
            new("grab_auto",
            [
                new SpriteLayer(HookJson, HookImageBase, 4),
                new SpriteLayer(HookJson, HookImageBase, 5),
            ]),

            // Movable-rail pieces (game HookMovable quads 6/8/7 = left cap, center tile, right cap). The
            // canvas assembles these into a rail of arbitrary length; they are never drawn centered like a
            // normal sprite, so the layer order here is left, center, right for the renderer to index.
            new("grab_rail",
            [
                new SpriteLayer(HookJson, HookImageBase, 6),
                new SpriteLayer(HookJson, HookImageBase, 8),
                new SpriteLayer(HookJson, HookImageBase, 7),
            ]),

            // Movable-rail hook (game HookMovable quad 10), drawn at the hook rest point in place of the
            // fixed hook when moveLength > 0. Not placeable; picked by grab state.
            new("grab_movable",
            [
                new SpriteLayer(HookJson, HookImageBase, 10),
            ]),

            // Highlighted movable-rail hook (game HookMovable quad 9), shown in place of grab_movable while
            // the mover is being dragged (game moverDragging != -1). Not placeable; picked by grab state.
            new("grab_movable_highlight",
            [
                new SpriteLayer(HookJson, HookImageBase, 9),
            ]),

            // Bubble = attached quad 0 over one random attached outline, matching LoadBubble's
            // parent quad RND_RANGE(1,3) plus child Image quad 0.
            new("bubble",
            [
                new SpriteLayer(BubbleJson, BubbleImageBase, 0),
            ],
            RandomBackLayers:
            [
                new SpriteLayer(BubbleJson, BubbleImageBase, 1),
                new SpriteLayer(BubbleJson, BubbleImageBase, 2),
                new SpriteLayer(BubbleJson, BubbleImageBase, 3),
            ]),

            // Star = glow halo (frame 0, the ImgObjStarIdleGlow quad) behind the star body. The body
            // idle loops frames 1-18 in game; frame 18 is the fullest front-on pose for a static view.
            new("star",
            [
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle", 0),
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle", 18),
            ]),

            // Timed star ring = empty ring behind full ring. The editor is static, so it draws the full
            // timer rather than clipping it to a countdown fraction.
            new("star_timed",
            [
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle", 20),
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle", 19),
            ]),

            // Gravity button.
            new("gravitySwitch",
            [
                new SpriteLayer("images/obj_star_idle.json", "images/obj_star_idle", 21),
            ]),

            // Light bulb = lantern bottle + top (the lit glow quad is omitted for a static thumbnail).
            new("lightBulb",
            [
                new SpriteLayer(LighterJson, LighterImageBase, 1),
                new SpriteLayer(LighterJson, LighterImageBase, 2),
            ]),

            // Lit-glow halo quad, drawn additively under the bottle at 1.5x litRadius by GlowDrawOperation.
            // A separate element so it is never composited into the static bulb thumbnail or selection bounds.
            new("lightBulb_glow",
            [
                new SpriteLayer(LighterJson, LighterImageBase, 0),
            ]),

            // Pump object
            new("pump",
            [
                new SpriteLayer(PumpJson, PumpImageBase, 0),
            ]),

            // Static spike quads. Game Spikes.GetSpikeTextureAndQuad maps width 1-4 to obj_spikes quads 8-11.
            new("spike1", [new SpriteLayer(SpikesJson, SpikesImageBase, 8)]),
            new("spike2", [new SpriteLayer(SpikesJson, SpikesImageBase, 9)]),
            new("spike3", [new SpriteLayer(SpikesJson, SpikesImageBase, 10)]),
            new("spike4", [new SpriteLayer(SpikesJson, SpikesImageBase, 11)]),

            // Rotatable spike quads. Group 0 has no embedded button; Game Spikes adds button quad 4 for
            // group 1 and quad 6 for group 2 as a child centered on the same obj_spikes sourceSize canvas.
            new("spike1_toggled_0", [new SpriteLayer(SpikesJson, SpikesImageBase, 0)]),
            new("spike2_toggled_0", [new SpriteLayer(SpikesJson, SpikesImageBase, 1)]),
            new("spike3_toggled_0", [new SpriteLayer(SpikesJson, SpikesImageBase, 2)]),
            new("spike4_toggled_0", [new SpriteLayer(SpikesJson, SpikesImageBase, 3)]),
            new("spike1_toggled_1", [new SpriteLayer(SpikesJson, SpikesImageBase, 0), new SpriteLayer(SpikesJson, SpikesImageBase, 4)]),
            new("spike2_toggled_1", [new SpriteLayer(SpikesJson, SpikesImageBase, 1), new SpriteLayer(SpikesJson, SpikesImageBase, 4)]),
            new("spike3_toggled_1", [new SpriteLayer(SpikesJson, SpikesImageBase, 2), new SpriteLayer(SpikesJson, SpikesImageBase, 4)]),
            new("spike4_toggled_1", [new SpriteLayer(SpikesJson, SpikesImageBase, 3), new SpriteLayer(SpikesJson, SpikesImageBase, 4)]),
            new("spike1_toggled_2", [new SpriteLayer(SpikesJson, SpikesImageBase, 0), new SpriteLayer(SpikesJson, SpikesImageBase, 6)]),
            new("spike2_toggled_2", [new SpriteLayer(SpikesJson, SpikesImageBase, 1), new SpriteLayer(SpikesJson, SpikesImageBase, 6)]),
            new("spike3_toggled_2", [new SpriteLayer(SpikesJson, SpikesImageBase, 2), new SpriteLayer(SpikesJson, SpikesImageBase, 6)]),
            new("spike4_toggled_2", [new SpriteLayer(SpikesJson, SpikesImageBase, 3), new SpriteLayer(SpikesJson, SpikesImageBase, 6)]),

            // Electric spark
            new("electro", [new SpriteLayer(ElectrodesJson, ElectrodesImageBase, 1)]),
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
