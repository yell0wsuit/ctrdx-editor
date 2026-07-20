using System;
using System.IO;

using CtrDxEditor.Views;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Drag-and-drop opening of level XML onto the editor window.</summary>
    public class MainViewLevelDropTests
    {
        /// <summary>A lone .xml is accepted regardless of how its extension is cased.</summary>
        [Theory]
        [InlineData("level.xml")]
        [InlineData("LEVEL.XML")]
        [InlineData("my.level.Xml")]
        public void AcceptsASingleXmlFile(string name)
        {
            Assert.True(MainView.AcceptsDroppedNames([name]));
        }

        /// <summary>Anything not ending in .xml is refused before the overlay ever appears.</summary>
        [Theory]
        [InlineData("screenshot.png")]
        [InlineData("level.xml.bak")]
        [InlineData("xml")]
        public void RejectsNonXmlFiles(string name)
        {
            Assert.False(MainView.AcceptsDroppedNames([name]));
        }

        /// <summary>A multi-file drop is ambiguous about which level to open, so nothing is accepted.</summary>
        [Fact]
        public void RejectsAnythingOtherThanExactlyOneFile()
        {
            Assert.False(MainView.AcceptsDroppedNames([]));
            Assert.False(MainView.AcceptsDroppedNames(["a.xml", "b.xml"]));
        }

        /// <summary>
        /// The TopLevel handlers outlive the view unless torn down, so every registration has a matching
        /// removal. Asserted against the source because there is no headless harness to attach the view in.
        /// </summary>
        [Fact]
        public void EveryDropHandlerRegistrationIsUndone()
        {
            string drop = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.DropCommands.cs"));
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));

            foreach (string handler in new[] { "DragOverEvent, Host_DragOver", "DragLeaveEvent, Host_DragLeave", "DropEvent, Host_Drop" })
            {
                Assert.Contains("AddHandler(DragDrop." + handler, drop, StringComparison.Ordinal);
                Assert.Contains("RemoveHandler(DragDrop." + handler, drop, StringComparison.Ordinal);
            }

            Assert.Contains("SetAllowDrop(top, true)", drop, StringComparison.Ordinal);
            Assert.Contains("SetAllowDrop(top, false)", drop, StringComparison.Ordinal);
            Assert.Contains("RegisterLevelDrop();", view, StringComparison.Ordinal);
            Assert.Contains("UnregisterLevelDrop();", view, StringComparison.Ordinal);
        }

        /// <summary>The overlay must not intercept the drag events the TopLevel handlers depend on.</summary>
        [Fact]
        public void DropOverlayIsNotHitTestable()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            int overlay = view.IndexOf("x:Name=\"DropOverlay\"", StringComparison.Ordinal);

            Assert.True(overlay >= 0, "MainView.axaml should declare the DropOverlay border.");
            Assert.Contains("IsHitTestVisible=\"False\"", view[overlay..], StringComparison.Ordinal);
        }

        /// <summary>
        /// A dialog's own drop must reach it. Both sets of handlers live on the same TopLevel and this
        /// view's run first, so handling the event unconditionally would stop the playtest locate dialog
        /// from ever seeing a dropped game bundle.
        /// </summary>
        [Fact]
        public void LevelDropStandsDownWhileADialogIsOpen()
        {
            string drop = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.DropCommands.cs"));
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            Assert.Contains("x:Name=\"DialogHost\"", view, StringComparison.Ordinal);
            Assert.Contains("FindControl<ReactiveDialogHost>(\"DialogHost\")?.IsOpen", drop, StringComparison.Ordinal);

            foreach (string handler in new[] { "Host_DragOver", "Host_Drop" })
            {
                int body = drop.IndexOf("void " + handler + "(", StringComparison.Ordinal);
                Assert.True(body >= 0, handler + " should exist in MainView.DropCommands.cs.");

                int guard = drop.IndexOf("if (DialogOwnsDrop)", body, StringComparison.Ordinal);
                int handled = drop.IndexOf("e.Handled = true;", body, StringComparison.Ordinal);
                Assert.True(guard >= 0 && guard < handled, handler + " should bail out before handling the event.");
            }
        }

        /// <summary>
        /// The locate dialog shares the window's drop with the editor, so its teardown restores what it
        /// found instead of switching drop off for whatever else is still listening.
        /// </summary>
        [Fact]
        public void LocateDialogRestoresTheHostsDropSetting()
        {
            string dialog = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "PlaytestLocateDialog.axaml.cs"));

            Assert.Contains("GetAllowDrop(top)", dialog, StringComparison.Ordinal);
            Assert.Contains("SetAllowDrop(top, _hostAllowedDrop)", dialog, StringComparison.Ordinal);
            Assert.DoesNotContain("SetAllowDrop(top, false)", dialog, StringComparison.Ordinal);
        }

        private static string SourcePath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine(path, Path.Combine(parts));
        }
    }
}
