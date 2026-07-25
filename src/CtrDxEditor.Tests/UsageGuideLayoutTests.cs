using CtrDxEditor.UsageGuide;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the guide's sidebar-to-drawer breakpoint independently of Avalonia layout.</summary>
    public class UsageGuideLayoutTests
    {
        /// <summary>A desktop-shaped guide keeps its table of contents open.</summary>
        [Fact]
        public void WideLandscapeUsesPersistentSidebar()
        {
            Assert.True(UsageGuideLayout.UsesPersistentSidebar(1100, 720));
        }

        /// <summary>A narrow landscape guide puts its contents behind the hamburger.</summary>
        [Fact]
        public void NarrowLandscapeUsesDrawer()
        {
            Assert.False(UsageGuideLayout.UsesPersistentSidebar(700, 500));
        }

        /// <summary>A portrait guide favors article width even when it clears the raw breakpoint.</summary>
        [Fact]
        public void TallPortraitUsesDrawer()
        {
            Assert.False(UsageGuideLayout.UsesPersistentSidebar(900, 1100));
        }

        /// <summary>The named sidebar breakpoint has stable boundary behavior.</summary>
        [Fact]
        public void SidebarBreakpointIsInclusive()
        {
            Assert.True(UsageGuideLayout.UsesPersistentSidebar(UsageGuideLayout.SidebarMinWidth, 600));
        }
    }
}
