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
    }
}
