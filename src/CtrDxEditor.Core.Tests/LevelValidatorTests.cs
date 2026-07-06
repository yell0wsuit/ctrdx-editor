using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the non-blocking level validation rules.</summary>
    public class LevelValidatorTests
    {
        private static LevelDocument Doc(string settingsFlags, string objects)
        {
            return LevelDocument.Parse($"""
            <map>
                <layer name="settings">
                    <map gridSize="32" width="320" height="480" />
                    <gameDesign {settingsFlags} />
                </layer>
                <layer name="Objects">{objects}</layer>
            </map>
            """);
        }

        /// <summary>Valid two-part levels produce no validation warnings.</summary>
        [Fact]
        public void ValidTwoPartLevelHasNoWarnings()
        {
            LevelDocument doc = Doc("twoParts=\"true\"",
                "<candyL x=\"1\" y=\"1\" /><candyR x=\"2\" y=\"2\" /><target x=\"3\" y=\"3\" />");

            Assert.Empty(LevelValidator.Validate(doc));
        }

        /// <summary>Two-part levels warn when either candy half is missing.</summary>
        [Fact]
        public void TwoPartMissingHalfWarns()
        {
            LevelDocument doc = Doc("twoParts=\"true\"",
                "<candyL x=\"1\" y=\"1\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("candy half"));
        }

        /// <summary>Two-part levels warn when they also contain a plain candy.</summary>
        [Fact]
        public void PlainCandyInTwoPartLevelWarns()
        {
            LevelDocument doc = Doc("twoParts=\"true\"",
                "<candyL x=\"1\" y=\"1\" /><candyR x=\"2\" y=\"2\" /><candy x=\"9\" y=\"9\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("plain candy"));
        }

        /// <summary>Single-candy levels warn when split-candy objects are present.</summary>
        [Fact]
        public void SplitCandyInSingleLevelWarns()
        {
            LevelDocument doc = Doc("twoParts=\"false\"",
                "<candy x=\"1\" y=\"1\" /><candyL x=\"5\" y=\"5\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("candyL/candyR"));
        }

        /// <summary>Night levels warn when no light bulb exists.</summary>
        [Fact]
        public void NightLevelWithoutBulbWarns()
        {
            LevelDocument doc = Doc("nightLevel=\"true\"",
                "<candy x=\"1\" y=\"1\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("light bulb"));
        }

        /// <summary>Levels warn when they are missing candy and Om Nom target objects.</summary>
        [Fact]
        public void MissingCandyAndTargetWarn()
        {
            LevelDocument doc = Doc("twoParts=\"false\"", "");

            IReadOnlyList<string> warnings = LevelValidator.Validate(doc);
            Assert.Contains(warnings, w => w.Contains("no candy"));
            Assert.Contains(warnings, w => w.Contains("Om Nom"));
        }

        /// <summary>Levels below the supported resolution floor produce a size warning.</summary>
        [Fact]
        public void UndersizedResolutionWarns()
        {
            LevelDocument doc = LevelDocument.Parse("""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="50" height="480" />
                        <gameDesign twoParts="false" />
                    </layer>
                    <layer name="Objects"><candy x="1" y="1" /><target x="2" y="2" /></layer>
                </map>
                """);

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("smaller than 320"));
        }

        /// <summary>Normal level resolutions do not produce size warnings.</summary>
        [Fact]
        public void NormalResolutionDoesNotWarnAboutSize()
        {
            LevelDocument doc = Doc("twoParts=\"false\"", "<candy x=\"1\" y=\"1\" /><target x=\"2\" y=\"2\" />");

            Assert.DoesNotContain(LevelValidator.Validate(doc), w => w.Contains("smaller than"));
        }

        /// <summary>Duplicate candyNumber values produce a validation warning.</summary>
        [Fact]
        public void WarnsOnDuplicateCandyNumbers()
        {
            LevelDocument doc = Doc("twoParts=\"false\"",
                "<target x=\"1\" y=\"1\" /><candy x=\"1\" y=\"1\" candyNumber=\"0\" /><candy x=\"2\" y=\"2\" candyNumber=\"0\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Grabs with a candyNumber that no candy has produce a warning.</summary>
        [Fact]
        public void WarnsOnGrabWithUnmatchedCandyNumber()
        {
            LevelDocument doc = Doc("twoParts=\"false\"",
                "<target x=\"1\" y=\"1\" /><candy x=\"1\" y=\"1\" candyNumber=\"0\" /><grab x=\"3\" y=\"3\" length=\"10\" candyNumber=\"7\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("candyNumber", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Bulb-bound grabs warn when their bulbNumber has no matching bulb.</summary>
        [Fact]
        public void WarnsOnBindBulbWithNoMatchingBulb()
        {
            LevelDocument doc = Doc("twoParts=\"false\"",
                "<target x=\"1\" y=\"1\" /><candy x=\"1\" y=\"1\" candyNumber=\"0\" /><grab x=\"3\" y=\"3\" bindBulb=\"true\" bulbNumber=\"5\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("bulbNumber", StringComparison.OrdinalIgnoreCase));
        }
    }
}
