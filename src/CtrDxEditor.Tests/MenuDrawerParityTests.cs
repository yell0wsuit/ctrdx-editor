using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Guards the two hand-written copies of the command set against drift.
    /// </summary>
    /// <remarks>
    /// The desktop menu and the compact drawer both list every command by hand. Until they share one
    /// action model, nothing stops a command being added to one and forgotten in the other. Comparing the
    /// localization keys each surface references catches that at build time.
    /// </remarks>
    public class MenuDrawerParityTests
    {
        /// <summary>Commands the drawer deliberately does not carry, with the surface that owns them.</summary>
        private static readonly Dictionary<string, string> DrawerExemptions = new(StringComparer.Ordinal)
        {
            ["Menu.Edit.Undo"] = "compact rail",
            ["Menu.Edit.Redo"] = "compact rail",
            ["Menu.View.ZoomFit"] = "compact rail",
            ["Menu.Edit.Cut"] = "compact edit bar",
            ["Menu.Edit.Copy"] = "compact edit bar",
            ["Menu.Edit.Paste"] = "compact edit bar",
            ["Menu.Edit.Delete"] = "compact edit bar",
        };

        /// <summary>Every desktop command appears in the drawer or on a named compact surface.</summary>
        [Fact]
        public void EveryDesktopCommandHasACompactHome()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            int drawerStart = view.IndexOf("x:Name=\"CompactCommandDrawer\"", StringComparison.Ordinal);
            int menuEnd = view.IndexOf("x:Name=\"CompactRail\"", StringComparison.Ordinal);

            Assert.True(drawerStart > 0 && menuEnd > 0);

            HashSet<string> menuKeys = CommandKeys(view[..menuEnd]);
            HashSet<string> compactKeys = CommandKeys(view[menuEnd..]);

            List<string> missing =
            [
                .. menuKeys
                    .Where(k => !compactKeys.Contains(k) && !DrawerExemptions.ContainsKey(k))
                    .OrderBy(k => k, StringComparer.Ordinal),
            ];

            Assert.True(
                missing.Count == 0,
                $"Desktop menu commands with no compact home: {string.Join(", ", missing)}. "
                + "Add them to the drawer, or add an entry to DrawerExemptions naming the surface that owns them.");
        }

        /// <summary>Exemptions name real commands, so the list cannot silently rot.</summary>
        [Fact]
        public void ExemptionsStillExistInTheDesktopMenu()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            int menuEnd = view.IndexOf("x:Name=\"CompactRail\"", StringComparison.Ordinal);
            HashSet<string> menuKeys = CommandKeys(view[..menuEnd]);

            foreach (string exempt in DrawerExemptions.Keys)
            {
                Assert.Contains(exempt, menuKeys);
            }
        }

        /// <summary>Grid and rotation snapping are editing behaviors, so both live under Edit rather than View.</summary>
        [Fact]
        public void SnapTogglesLiveInTheEditMenu()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            int editStart = view.IndexOf("<MenuItem Header=\"{loc:Tr Menu.Edit}\">", StringComparison.Ordinal);
            int viewStart = view.IndexOf("<MenuItem Header=\"{loc:Tr Menu.View}\">", editStart, StringComparison.Ordinal);
            int menuEnd = view.IndexOf("</Menu>", viewStart, StringComparison.Ordinal);

            Assert.True(editStart >= 0 && viewStart > editStart && menuEnd > viewStart);
            string editMenu = view[editStart..viewStart];
            string viewMenu = view[viewStart..menuEnd];

            Assert.Contains("Click=\"SnapToggle_Click\"", editMenu, StringComparison.Ordinal);
            Assert.Contains("Menu.Edit.SnapToGrid", editMenu, StringComparison.Ordinal);
            Assert.Contains("Click=\"RotationSnapToggle_Click\"", editMenu, StringComparison.Ordinal);
            Assert.Contains("Menu.Edit.SnapRotation", editMenu, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding SnapEnabled}\"", editMenu, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding RotationSnapEnabled}\"", editMenu, StringComparison.Ordinal);
            Assert.DoesNotContain("SnapToggle_Click", viewMenu, StringComparison.Ordinal);
            Assert.DoesNotContain("SnapToGrid", viewMenu, StringComparison.Ordinal);
        }

        private static HashSet<string> CommandKeys(string markup)
        {
            return Regex.Matches(markup, @"loc:Tr (Menu\.(?:File|Edit|View)\.[A-Za-z]+)")
                .Select(m => m.Groups[1].Value)
                .ToHashSet(StringComparer.Ordinal);
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
