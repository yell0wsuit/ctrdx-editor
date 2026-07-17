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
        private const string BouncerJson = "images/obj_bouncer.json";
        private const string BouncerImageBase = "images/obj_bouncer";
        private const string GhostJson = "images/obj_ghost.json";
        private const string GhostImageBase = "images/obj_ghost";
        private const string PipeJson = "images/obj_pipe.json";
        private const string PipeImageBase = "images/obj_pipe";
        private const string SpikesJson = "images/obj_spikes.json";
        private const string SpikesImageBase = "images/obj_spikes";
        private const string ElectrodesJson = "images/obj_electrodes.json";
        private const string ElectrodesImageBase = "images/obj_electrodes";
        private const string HatJson = "images/obj_hat.json";
        private const string HatImageBase = "images/obj_hat";
        private const string XmasSockJson = "images/obj_sock_xmas.json";
        private const string XmasSockImageBase = "images/obj_sock_xmas";
        private const string BeeJson = "images/obj_bee.json";
        private const string BeeImageBase = "images/obj_bee";
        private const string VinylJson = "images/obj_vinil.json";
        private const string VinylImageBase = "images/obj_vinil";
        private const string LanternJson = "images/obj_lantern.json";
        private const string LanternImageBase = "images/obj_lantern";
        private const string MouseJson = "images/obj_mouse.json";
        private const string MouseImageBase = "images/obj_mouse";
        private const string ConveyorJson = "images/obj_conveyor.json";
        private const string ConveyorImageBase = "images/obj_conveyor";
        private const string RocketJson = "images/obj_rocket.json";
        private const string RocketImageBase = "images/obj_rocket";
        private const string SnailJson = "images/obj_snail.json";
        private const string SnailImageBase = "images/obj_snail";
        private const string TutorialSignsJson = "images/tutorial_signs.json";
        private const string TutorialSignsImageBase = "images/tutorial_signs";

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

            new("grab_bee_body", [new SpriteLayer(BeeJson, BeeImageBase, 1)], Scale: 0.77),
            new("grab_bee_wing_0", [new SpriteLayer(BeeJson, BeeImageBase, 2)], Scale: 0.77),
            new("grab_bee_wing_1", [new SpriteLayer(BeeJson, BeeImageBase, 3)], Scale: 0.77),
            new("grab_bee_wing_2", [new SpriteLayer(BeeJson, BeeImageBase, 4)], Scale: 0.77),
            new("grab_pollen", [new SpriteLayer(BeeJson, BeeImageBase, 5)]),

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

            // Idle lantern body (lantern_start, quad 2). Palette thumbnail + empty-lantern canvas art.
            new("lantern", [new SpriteLayer(LanternJson, LanternImageBase, 2)]),

            // Active lantern: fire glow (quad 0) behind the lit body (lantern_end, quad 1). The skinned
            // inner candy is drawn separately by GetLanternInnerCandy (skin-dependent), not composited here.
            new("lantern_active",
            [
                new SpriteLayer(LanternJson, LanternImageBase, 0),
                new SpriteLayer(LanternJson, LanternImageBase, 1),
            ]),

            // Mouse (game element gap/mouse). Layer 0 is the static hole (Mouse.HoleQuad) drawn
            // upright; layers 1-2 are the idle mouse body (Mouse.IdleQuad) and its open eyes
            // (Mouse.EyesStartQuad) that DrawObject rotates together by the authored angle, matching
            // Mouse.Update which rotates the body/eyes container but not the hole.
            new("gap",
            [
                new SpriteLayer(MouseJson, MouseImageBase, 0),
                new SpriteLayer(MouseJson, MouseImageBase, 14)
            ]),

            // Conveyor palette thumbnail (game element `transporter`). A short belt segment: middle
            // background (quad 2) + moving plate (4) + directional arrow (5) + highlight (6). The belt is
            // custom-rendered on the canvas by ConveyorRenderer from `transporter_belt` below, so this
            // descriptor exists only to give the palette a recognizable icon.
            new("transporter",
            [
                new SpriteLayer(ConveyorJson, ConveyorImageBase, 2),
                new SpriteLayer(ConveyorJson, ConveyorImageBase, 4),
                new SpriteLayer(ConveyorJson, ConveyorImageBase, 5),
                new SpriteLayer(ConveyorJson, ConveyorImageBase, 6),
            ]),

            // Conveyor belt pieces for ConveyorRenderer (not placeable). Ordered to match the quad indices
            // in the game's ConveyorBelt.cs so the renderer indexes them directly:
            // 0 end, 1 end-side, 2 middle, 3 middle-side, 4 plate, 5 plate-arrow, 6 highlight.
            new("transporter_belt",
            [
                new SpriteLayer(ConveyorJson, ConveyorImageBase, 0),
                new SpriteLayer(ConveyorJson, ConveyorImageBase, 1),
                new SpriteLayer(ConveyorJson, ConveyorImageBase, 2),
                new SpriteLayer(ConveyorJson, ConveyorImageBase, 3),
                new SpriteLayer(ConveyorJson, ConveyorImageBase, 4),
                new SpriteLayer(ConveyorJson, ConveyorImageBase, 5),
                new SpriteLayer(ConveyorJson, ConveyorImageBase, 6),
            ]),

            // Rocket body (obj_rocket quad 10, frame_10_rocket), drawn at the game's 0.7 scale. Palette
            // thumbnail + the rotated body on canvas.
            new("rocket", [new SpriteLayer(RocketJson, RocketImageBase, 10)], Scale: 0.7),

            // Rocket launcher base (quad 0, frame_00_launcher). Non-placeable; drawn upright behind the
            // body only when isRotatable, matching LoadRocket's decalsLayer marker. The game leaves the
            // marker Image at the default scale 1.0 (only the rocket body is scaled to 0.7), so the base
            // renders larger than the body.
            new("rocket_launcher", [new SpriteLayer(RocketJson, RocketImageBase, 0)]),

            // Snail at rest (Snail.InitWithTexture leaves it in SNAIL_STATE_INACTIVE): the sleepy eyes
            // (quad 2) sit in backContainer behind the shell (quad 8). Both share obj_snail's 393x418
            // canvas, so their trims align with no manual offset. The spawn/pulse scale timelines are
            // runtime-only, so the palette and canvas draw at scale 1.
            new("load",
            [
                new SpriteLayer(SnailJson, SnailImageBase, 2),
                new SpriteLayer(SnailJson, SnailImageBase, 8),
            ]),

            // Tutorial icons map tutorial01..tutorial11 to tutorial_signs quads 0..10.
            new("tutorial01", [new SpriteLayer(TutorialSignsJson, TutorialSignsImageBase, 0)]),
            new("tutorial02", [new SpriteLayer(TutorialSignsJson, TutorialSignsImageBase, 1)]),
            new("tutorial03", [new SpriteLayer(TutorialSignsJson, TutorialSignsImageBase, 2)]),
            new("tutorial04", [new SpriteLayer(TutorialSignsJson, TutorialSignsImageBase, 3)]),
            new("tutorial05", [new SpriteLayer(TutorialSignsJson, TutorialSignsImageBase, 4)]),
            new("tutorial06", [new SpriteLayer(TutorialSignsJson, TutorialSignsImageBase, 5)]),
            new("tutorial07", [new SpriteLayer(TutorialSignsJson, TutorialSignsImageBase, 6)]),
            new("tutorial08", [new SpriteLayer(TutorialSignsJson, TutorialSignsImageBase, 7)]),
            new("tutorial09", [new SpriteLayer(TutorialSignsJson, TutorialSignsImageBase, 8)]),
            new("tutorial10", [new SpriteLayer(TutorialSignsJson, TutorialSignsImageBase, 9)]),
            new("tutorial11", [new SpriteLayer(TutorialSignsJson, TutorialSignsImageBase, 10)]),

            // Pump object
            new("pump",
            [
                new SpriteLayer(PumpJson, PumpImageBase, 0),
            ]),

            // Bouncers animate over quads 0-4 (small) and 5-9 (large); use each range's resting frame.
            new("bouncer1", [new SpriteLayer(BouncerJson, BouncerImageBase, 0)]),
            new("bouncer2", [new SpriteLayer(BouncerJson, BouncerImageBase, 5)]),

            // Ghost = body (quad 0) + face (quad 1), both center-anchored (game anchor 18).
            // Morph sprites are reused from their own descriptors by the selection preview.
            new("ghost",
            [
                new SpriteLayer(GhostJson, GhostImageBase, 0),
                new SpriteLayer(GhostJson, GhostImageBase, 1),
            ]),

            // Steam Pipe palette sprite: body and valve only. The renderer applies the valve's game offset;
            // the separate fixed puff is canvas-only so thumbnails never include steam.
            new("steamTube",
            [
                new SpriteLayer(PipeJson, PipeImageBase, 0),
                new SpriteLayer(PipeJson, PipeImageBase, 1),
            ]),

            // All three 11-frame puff loops (2-12, 13-23, 24-34). The canvas freezes a steady-state
            // 20-puff maximum plume by indexing these layers; none are part of the palette thumbnail.
            new("steamTube_puffs", PipePuffLayers()),

            // Magic hat teleporter. LoadSock uses quad 0 for group 0 and quad 1 otherwise,
            // swaps to the Christmas sock atlas during the seasonal event, and scales it to 0.7.
            new("sock", [new SpriteLayer(HatJson, HatImageBase, 0)], Scale: 0.7),
            new("sock_grouped", [new SpriteLayer(HatJson, HatImageBase, 1)], Scale: 0.7),
            new("sock_xmas", [new SpriteLayer(XmasSockJson, XmasSockImageBase, 0)], Scale: 0.7),
            new("sock_xmas_grouped", [new SpriteLayer(XmasSockJson, XmasSockImageBase, 1)], Scale: 0.7),

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

            // Electric sparks
            new("electro", [new SpriteLayer(ElectrodesJson, ElectrodesImageBase, 1)]),
            new("electro_off", [new SpriteLayer(ElectrodesJson, ElectrodesImageBase, 0)]),
            new("electro_on_1", [new SpriteLayer(ElectrodesJson, ElectrodesImageBase, 1)]),
            new("electro_on_2", [new SpriteLayer(ElectrodesJson, ElectrodesImageBase, 2)]),
            new("electro_on_3", [new SpriteLayer(ElectrodesJson, ElectrodesImageBase, 3)]),
            new("electro_on_4", [new SpriteLayer(ElectrodesJson, ElectrodesImageBase, 4)]),

            // Vinyl (rotatedCircle) = disc body (quad 0) + center spindle dot (quad 3). The DJ disc scales
            // with its size attribute, so DrawVinyl applies a per-object scale rather than this fixed one;
            // this descriptor drives the palette thumbnail and supplies the body/center art.
            new("rotatedCircle",
            [
                new SpriteLayer(VinylJson, VinylImageBase, 0),
                new SpriteLayer(VinylJson, VinylImageBase, 3),
            ]),

            // Vinyl highlight half (quad 1) and label half (quad 2). Each is one authored half; DrawVinyl
            // draws it plus its horizontal mirror to form the full symmetric sheen / label. Not placeable.
            new("vinyl_highlight", [new SpriteLayer(VinylJson, VinylImageBase, 1)]),
            new("vinyl_sticker", [new SpriteLayer(VinylJson, VinylImageBase, 2)]),

            // Vinyl handle (game controller, quad 5), drawn by DrawVinyl at each handle position. Not placeable.
            new("vinyl_handle", [new SpriteLayer(VinylJson, VinylImageBase, 5)]),

            // Active/operated controller glow (quad 4), drawn behind the handle being dragged or hovered,
            // matching the game's vinilActiveController. Not placeable.
            new("vinyl_active_controller", [new SpriteLayer(VinylJson, VinylImageBase, 4)]),
        ];

        private static IReadOnlyList<SpriteLayer> PipePuffLayers()
        {
            return [.. Enumerable.Range(2, 33).Select(quad => new SpriteLayer(PipeJson, PipeImageBase, quad))];
        }

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
