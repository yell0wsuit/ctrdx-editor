using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the compact command drawer's markup, sizing and dismissal wiring.</summary>
    public class CompactCommandDrawerTests
    {
        /// <summary>The hamburger and native Fluent drawer exist, with no standalone scrim to pop in and out.</summary>
        [Fact]
        public void DrawerUsesNativeFluentSplitView()
        {
            string view = SourceText("MainView.axaml");
            int drawer = view.IndexOf("<SplitView x:Name=\"CompactCommandDrawer\"", StringComparison.Ordinal);
            int drawerTagEnd = view.IndexOf('>', drawer);

            Assert.Contains("x:Name=\"CompactMenuButton\"", view, StringComparison.Ordinal);
            Assert.True(drawer >= 0 && drawerTagEnd > drawer);
            ReadOnlySpan<char> drawerTag = view.AsSpan(drawer, drawerTagEnd - drawer);
            Assert.Contains("DisplayMode=\"Overlay\"", drawerTag, StringComparison.Ordinal);
            Assert.Contains("PanePlacement=\"Left\"", drawerTag, StringComparison.Ordinal);
            Assert.Contains("UseLightDismissOverlayMode=\"True\"", drawerTag, StringComparison.Ordinal);
            Assert.Contains("IsHitTestVisible=\"False\"", drawerTag, StringComparison.Ordinal);
            Assert.DoesNotContain("x:Name=\"CompactCommandScrim\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"CommandDrawerFileSection\"", view, StringComparison.Ordinal);
        }

        /// <summary>
        /// The drawer never uses a nested Menu: MenuItem carries Fluent's pointer-sized metrics and
        /// flyout behaviour, which is the whole reason the compact shell is replacing it.
        /// </summary>
        [Fact]
        public void DrawerDoesNotNestAMenu()
        {
            string view = SourceText("MainView.axaml");
            int drawer = view.IndexOf("x:Name=\"CompactCommandDrawer\"", StringComparison.Ordinal);

            Assert.True(drawer >= 0);
            string markup = view[drawer..];
            int end = markup.IndexOf("x:Name=\"CompactMenuButton\"", StringComparison.Ordinal);

            Assert.DoesNotContain("<MenuItem", markup[..end], StringComparison.Ordinal);
        }

        /// <summary>
        /// Width leaves usable canvas behind an open drawer, because toggles keep it open and their
        /// whole point is watching the canvas change.
        /// </summary>
        [Fact]
        public void DrawerWidthLeavesCanvasVisible()
        {
            string layout = SourceText("MainView.Layout.cs");

            Assert.Contains("Math.Min(260, Bounds.Width * 0.70)", layout, StringComparison.Ordinal);
        }

        /// <summary>The drawer surface fills the native pane instead of leaving a strip on its right.</summary>
        [Fact]
        public void DrawerSurfaceStretchesToTheNativePaneWidth()
        {
            string view = SourceText("MainView.axaml");
            int pane = view.IndexOf("<Border x:Name=\"CompactCommandDrawerPane\"", StringComparison.Ordinal);
            int paneTagEnd = view.IndexOf('>', pane);

            Assert.True(pane >= 0 && paneTagEnd > pane);
            Assert.Contains(
                "HorizontalAlignment=\"Stretch\"",
                view.AsSpan(pane, paneTagEnd - pane),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Android's back button in a browser is a history popstate, not a key event, and iOS has no
        /// back key at all. Wiring them produces tests that pass and behaviour that never fires.
        /// </summary>
        [Fact]
        public void DrawerDoesNotWireBrowserBackKeys()
        {
            string drawer = SourceText("MainView.CommandDrawer.cs");

            Assert.DoesNotContain("Key.BrowserBack", drawer, StringComparison.Ordinal);
            Assert.DoesNotContain("Key.Back", drawer, StringComparison.Ordinal);
            Assert.Contains("Key.Escape", drawer, StringComparison.Ordinal);
        }

        /// <summary>
        /// A left-edge swipe is Safari's back gesture on iOS, so the hamburger is the only way in.
        /// </summary>
        [Fact]
        public void DrawerHasNoEdgeSwipeGesture()
        {
            string drawer = SourceText("MainView.CommandDrawer.cs");

            Assert.DoesNotContain("Gesture", drawer, StringComparison.Ordinal);
            Assert.DoesNotContain("Swipe", drawer, StringComparison.Ordinal);
        }

        /// <summary>Every programmatic dismissal path funnels through one method, so states cannot drift.</summary>
        [Fact]
        public void ProgrammaticDismissalPathsUseOneMethod()
        {
            string drawer = SourceText("MainView.CommandDrawer.cs");

            Assert.Contains("private void SetCommandDrawerOpen(bool open, bool restoreFocus = true)", drawer, StringComparison.Ordinal);
            Assert.DoesNotContain("scrim.IsVisible =", drawer, StringComparison.Ordinal);
            Assert.Contains("drawer.IsPaneOpen = open;", drawer, StringComparison.Ordinal);
        }

        /// <summary>A canvas press closes the command drawer before it considers the bottom sheet.</summary>
        [Fact]
        public void CanvasPressClosesCommandDrawerFirst()
        {
            string layout = SourceText("MainView.Layout.cs");
            int method = layout.IndexOf("private bool DismissCompactDrawerOnCanvasPress", StringComparison.Ordinal);

            Assert.True(method >= 0);
            int close = layout.IndexOf("SetCommandDrawerOpen(false", method, StringComparison.Ordinal);
            int sheet = layout.IndexOf("CompactSheet", method, StringComparison.Ordinal);

            Assert.True(close >= 0 && close < sheet);
        }

        /// <summary>The edit bar stands down while the drawer covers it.</summary>
        [Fact]
        public void EditBarHidesWhileTheDrawerIsOpen()
        {
            string layout = SourceText("MainView.Layout.cs");
            int method = layout.IndexOf("private void UpdateCompactEditBarVisibility", StringComparison.Ordinal);

            Assert.True(method >= 0);
            int end = layout.IndexOf("\n        }", method, StringComparison.Ordinal);
            Assert.Contains("!IsCommandDrawerOpen", layout[method..end], StringComparison.Ordinal);
        }

        /// <summary>All three sections are present, in the same order as the desktop menu.</summary>
        [Fact]
        public void DrawerHasFileEditAndViewSections()
        {
            string view = SourceText("MainView.axaml");
            int file = view.IndexOf("x:Name=\"CommandDrawerFileSection\"", StringComparison.Ordinal);
            int edit = view.IndexOf("x:Name=\"CommandDrawerEditSection\"", StringComparison.Ordinal);
            int viewSection = view.IndexOf("x:Name=\"CommandDrawerViewSection\"", StringComparison.Ordinal);

            Assert.True(file >= 0);
            Assert.True(edit > file);
            Assert.True(viewSection > edit);
        }

        /// <summary>
        /// Toggles keep the drawer open because their effect is on the canvas behind it; one-shot
        /// commands close it first so the drawer is not left covering the dialog they open.
        /// </summary>
        [Fact]
        public void OneShotRowsCloseTheDrawerAndTogglesDoNot()
        {
            string drawer = SourceText("MainView.CommandDrawer.cs");
            string view = SourceText("MainView.axaml");

            Assert.Contains("private void CloseDrawerThen(", drawer, StringComparison.Ordinal);
            Assert.Contains("Click=\"DrawerSnapToggle_Click\"", view, StringComparison.Ordinal);
            Assert.Contains("Click=\"DrawerShowHitboxes_Click\"", view, StringComparison.Ordinal);
            Assert.Contains("Click=\"DrawerZoomIn_Click\"", view, StringComparison.Ordinal);

            int snap = drawer.IndexOf("DrawerSnapToggle_Click", StringComparison.Ordinal);
            int snapEnd = drawer.IndexOf("private void", snap + 10, StringComparison.Ordinal);
            Assert.DoesNotContain("CloseDrawerThen", drawer[snap..snapEnd], StringComparison.Ordinal);

            int zoom = drawer.IndexOf("DrawerZoomIn_Click", StringComparison.Ordinal);
            int zoomEnd = drawer.IndexOf("private void", zoom + 10, StringComparison.Ordinal);
            Assert.Contains("CloseDrawerThen", drawer[zoom..zoomEnd], StringComparison.Ordinal);
        }

        /// <summary>Toggle rows show their state, matching the desktop menu's check treatment.</summary>
        [Fact]
        public void ToggleRowsShowACheck()
        {
            string view = SourceText("MainView.axaml");
            int editSection = view.IndexOf("x:Name=\"CommandDrawerEditSection\"", StringComparison.Ordinal);
            int viewSection = view.IndexOf("x:Name=\"CommandDrawerViewSection\"", StringComparison.Ordinal);

            Assert.True(editSection >= 0 && viewSection > editSection);
            string editMarkup = view[editSection..viewSection];
            string viewMarkup = view[viewSection..];

            Assert.Contains("IsVisible=\"{Binding SnapEnabled}\"", editMarkup, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding ShowHitboxes}\"", viewMarkup, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding ShowForceFields}\"", viewMarkup, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding ShowMovementPaths}\"", viewMarkup, StringComparison.Ordinal);
        }

        /// <summary>Grid snap follows its desktop command into the drawer's Edit section.</summary>
        [Fact]
        public void GridSnapLivesInDrawerEditSection()
        {
            string view = SourceText("MainView.axaml");
            int editStart = view.IndexOf("x:Name=\"CommandDrawerEditSection\"", StringComparison.Ordinal);
            int viewStart = view.IndexOf("x:Name=\"CommandDrawerViewSection\"", editStart, StringComparison.Ordinal);

            Assert.True(editStart >= 0 && viewStart > editStart);
            Assert.Contains("DrawerSnapToggle_Click", view[editStart..viewStart], StringComparison.Ordinal);
            Assert.DoesNotContain("DrawerSnapToggle_Click", view[viewStart..], StringComparison.Ordinal);
        }

        /// <summary>The drawer reserves a header row so the higher-Z hamburger covers no command content.</summary>
        [Fact]
        public void DrawerContentClearsTheHamburger()
        {
            string view = SourceText("MainView.axaml");
            int drawer = view.IndexOf("x:Name=\"CompactCommandDrawer\"", StringComparison.Ordinal);
            int button = view.IndexOf("x:Name=\"CompactMenuButton\"", drawer, StringComparison.Ordinal);

            Assert.True(drawer >= 0 && button > drawer);
            Assert.Contains("<Grid RowDefinitions=\"48,*\">", view[drawer..button], StringComparison.Ordinal);
            Assert.Contains("<ScrollViewer Grid.Row=\"1\"", view[drawer..button], StringComparison.Ordinal);
        }

        /// <summary>One-shot rows restore focus to the still-visible hamburger before running.</summary>
        [Fact]
        public void OneShotRowsRestoreHamburgerFocus()
        {
            string drawer = SourceText("MainView.CommandDrawer.cs");
            int method = drawer.IndexOf("private void CloseDrawerThen", StringComparison.Ordinal);
            int end = drawer.IndexOf("\n        }", method, StringComparison.Ordinal);

            Assert.True(method >= 0 && end > method);
            string body = drawer[method..end];
            Assert.Contains("SetCommandDrawerOpen(false);", body, StringComparison.Ordinal);
            Assert.DoesNotContain("restoreFocus: false", body, StringComparison.Ordinal);
        }

        /// <summary>Widening an open drawer moves focus to the expanded canvas, not a hidden row.</summary>
        [Fact]
        public void ExpandedResizeMovesDrawerFocusToCanvas()
        {
            string layout = SourceText("MainView.Layout.cs");
            int expanded = layout.IndexOf("else\n            {", StringComparison.Ordinal);
            int end = layout.IndexOf("\n            }", expanded, StringComparison.Ordinal);

            Assert.True(expanded >= 0 && end > expanded);
            string branch = layout[expanded..end];
            Assert.Contains("bool focusCanvas = IsCommandDrawerOpen;", branch, StringComparison.Ordinal);
            Assert.Contains("_ = _canvas.Focus();", branch, StringComparison.Ordinal);
        }

        private static string SourceText(string file)
        {
            return File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", file));
        }

        private static string SourcePath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine([path, .. parts]);
        }
    }
}
