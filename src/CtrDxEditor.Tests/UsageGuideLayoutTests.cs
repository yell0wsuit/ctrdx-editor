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

        /// <summary>A narrow guide stacks search beneath navigation before the toolbar becomes cramped.</summary>
        [Fact]
        public void NarrowGuideUsesStackedToolbar()
        {
            Assert.True(UsageGuideLayout.UsesStackedToolbar(620));
        }

        /// <summary>A roomy guide keeps navigation and search together in one toolbar row.</summary>
        [Fact]
        public void WideGuideUsesSingleRowToolbar()
        {
            Assert.False(UsageGuideLayout.UsesStackedToolbar(900));
        }

        /// <summary>The named toolbar breakpoint has stable boundary behavior.</summary>
        [Fact]
        public void ToolbarBreakpointIsInclusive()
        {
            Assert.False(UsageGuideLayout.UsesStackedToolbar(UsageGuideLayout.ToolbarMinWidth));
        }
    }
}
