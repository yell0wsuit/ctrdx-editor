using System.Linq;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the grab hook sprites: the fixed hook and the auto-catch (auto-hook) variant.</summary>
    public class GrabSpriteTests
    {
        /// <summary>The fixed grab uses the normal hook frames (game quads 0/1).</summary>
        [Fact]
        public void FixedGrabUsesFixedHookFrames()
        {
            VisualDescriptor grab = VisualDescriptorMap.For("grab")!;

            Assert.Equal(
                ["obj_hook_01_frame_0000.png", "obj_hook_01_frame_0001.png"],
                grab.Layers.Select(l => l.FrameName));
        }

        /// <summary>The auto-catch grab uses the auto-hook frames (game HookAuto quads 4/5).</summary>
        [Fact]
        public void AutoCatchGrabUsesAutoHookFrames()
        {
            VisualDescriptor grabAuto = VisualDescriptorMap.For("grab_auto")!;

            Assert.Equal(
                ["obj_hook_auto_frame_0000.png", "obj_hook_auto_frame_0001.png"],
                grabAuto.Layers.Select(l => l.FrameName));
            Assert.All(grabAuto.Layers, l => Assert.Equal("images/obj_hook.json", l.AtlasJsonRelPath));
        }

        /// <summary>The rail is assembled from the movable left cap, center tile, and right cap, in that order.</summary>
        [Fact]
        public void MovableRailUsesCapAndTileFrames()
        {
            VisualDescriptor rail = VisualDescriptorMap.For("grab_rail")!;

            Assert.Equal(
                [
                    "obj_hook_movable_frame_0000.png", // left cap
                    "obj_hook_movable_frame_0002.png", // center tile
                    "obj_hook_movable_frame_0001.png", // right cap
                ],
                rail.Layers.Select(l => l.FrameName));
        }

        /// <summary>The movable hook uses the movable hook frame (game HookMovable quad 10).</summary>
        [Fact]
        public void MovableGrabUsesMovableHookFrame()
        {
            VisualDescriptor movable = VisualDescriptorMap.For("grab_movable")!;

            Assert.Equal(["obj_hook_movable_frame_0004.png"], movable.Layers.Select(l => l.FrameName));
        }
    }
}
