using System;
using System.IO;
using System.Text.RegularExpressions;

using CtrDxEditor.Localization;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests modal help in the non-customization portion of Level Settings.</summary>
    public class LevelSettingsHelpTests
    {
        private static readonly string[] HelpPairs =
        [
            "LevelName|LevelNameDescription",
            "Resolution|ResolutionDescription",
            "RopePhysicsSpeed|RopePhysicsSpeedDescription",
            "Gravity|GravityDescription",
            "HalfCandy|HalfCandyDescription",
            "NightLevel|NightLevelDescription",
            "MobilePhysics|MobilePhysicsDescription",
            "TimeTravelRocketPhysics|TimeTravelRocketPhysicsDescription",
            "Water|WaterDescription",
            "WaterSpeed|WaterSpeedDescription",
        ];

        /// <summary>Each regular setting has one help button, while customization remains uncluttered.</summary>
        [Fact]
        public void HelpButtonsCoverOnlyNonCustomizationSettings()
        {
            string markup = ReadDialog();
            int customization = markup.IndexOf(
                "Text=\"{loc:Tr Dialog.LevelSettings.CustomizationOptions}\"", StringComparison.Ordinal);
            Assert.True(customization > 0);

            string settings = markup[..customization];
            string customizationOptions = markup[customization..];

            Assert.Equal(HelpPairs.Length, Regex.Count(settings, @"<ctrl:HelpButton\b"));
            Assert.DoesNotContain("<ctrl:HelpButton", customizationOptions, StringComparison.Ordinal);

            foreach (string pair in HelpPairs)
            {
                string[] keys = pair.Split('|');
                Assert.Contains($"Header=\"{{loc:Tr Dialog.LevelSettings.{keys[0]}}}\"", settings,
                    StringComparison.Ordinal);
                Assert.Contains($"Message=\"{{loc:Tr Dialog.LevelSettings.{keys[1]}}}\"", settings,
                    StringComparison.Ordinal);
            }
        }

        /// <summary>Contextual documentation no longer depends on hover or consumes permanent dialog space.</summary>
        [Fact]
        public void HelpButtonsReplaceTooltipsAndStaticDescriptions()
        {
            string markup = ReadDialog();

            Assert.DoesNotContain("ToolTip.Tip", markup, StringComparison.Ordinal);
            foreach (string pair in HelpPairs)
            {
                string description = pair.Split('|')[1];
                Assert.DoesNotContain($"Text=\"{{loc:Tr Dialog.LevelSettings.{description}}}\"", markup,
                    StringComparison.Ordinal);
            }

            Assert.Contains("Text=\"{Binding WaterDrainHint}\"", markup, StringComparison.Ordinal);
        }

        /// <summary>Every dialog body resolves from localization rather than exposing its key.</summary>
        [Fact]
        public void HelpDescriptionsAreLocalized()
        {
            foreach (string pair in HelpPairs)
            {
                string description = pair.Split('|')[1];
                string key = $"Dialog.LevelSettings.{description}";
                Assert.NotEqual(key, Localizer.Get(key));
            }
        }

        private static string ReadDialog()
        {
            return File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "LevelSettingsDialog.axaml"));
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
