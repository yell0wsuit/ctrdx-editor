using System.Collections.Generic;
using System.Linq;

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
                "<candyL x=\"1\" y=\"1\" /><candyR x=\"2\" y=\"2\" /><target x=\"300\" y=\"300\" />");

            Assert.Empty(LevelValidator.Validate(doc));
        }

        /// <summary>Two-part levels warn when either candy half is missing.</summary>
        [Fact]
        public void TwoPartMissingHalfWarns()
        {
            LevelDocument doc = Doc("twoParts=\"true\"",
                "<candyL x=\"1\" y=\"1\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.TwoPartMissingHalf");
        }

        /// <summary>Two-part levels warn when they also contain a plain candy.</summary>
        [Fact]
        public void PlainCandyInTwoPartLevelWarns()
        {
            LevelDocument doc = Doc("twoParts=\"true\"",
                "<candyL x=\"1\" y=\"1\" /><candyR x=\"2\" y=\"2\" /><candy x=\"9\" y=\"9\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.TwoPartHasPlainCandy");
        }

        /// <summary>Single-candy levels warn when split-candy objects are present.</summary>
        [Fact]
        public void SplitCandyInSingleLevelWarns()
        {
            LevelDocument doc = Doc("twoParts=\"false\"",
                "<candy x=\"1\" y=\"1\" /><candyL x=\"5\" y=\"5\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.SingleCandyHasHalves");
        }

        /// <summary>Night levels warn when no light bulb exists.</summary>
        [Fact]
        public void NightLevelWithoutBulbWarns()
        {
            LevelDocument doc = Doc("nightLevel=\"true\"",
                "<candy x=\"1\" y=\"1\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.NightNoBulb");
        }

        /// <summary>Levels warn when they are missing candy and Om Nom target objects.</summary>
        [Fact]
        public void MissingCandyAndTargetWarn()
        {
            LevelDocument doc = Doc("twoParts=\"false\"", "");

            IReadOnlyList<LevelWarning> warnings = LevelValidator.Validate(doc);
            Assert.Contains(warnings, w => w.Key == "Validation.NoCandy");
            Assert.Contains(warnings, w => w.Key == "Validation.NoTarget");
        }

        /// <summary>A captured lantern supplies the implicit primary candy, so no "no candy" warning fires.</summary>
        [Fact]
        public void CapturedLanternSatisfiesCandyPresence()
        {
            LevelDocument doc = Doc("twoParts=\"false\"",
                "<target x=\"1\" y=\"1\" /><lantern x=\"2\" y=\"2\" candyCaptured=\"true\" />");

            Assert.DoesNotContain(LevelValidator.Validate(doc), w => w.Key == "Validation.NoCandy");
        }

        /// <summary>An idle lantern feeds nothing, so a candy-less level still warns.</summary>
        [Fact]
        public void IdleLanternDoesNotSatisfyCandyPresence()
        {
            LevelDocument doc = Doc("twoParts=\"false\"",
                "<target x=\"1\" y=\"1\" /><lantern x=\"2\" y=\"2\" candyCaptured=\"false\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.NoCandy");
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

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.ResolutionTooSmall");
        }

        /// <summary>Normal level resolutions do not produce size warnings.</summary>
        [Fact]
        public void NormalResolutionDoesNotWarnAboutSize()
        {
            LevelDocument doc = Doc("twoParts=\"false\"", "<candy x=\"1\" y=\"1\" /><target x=\"2\" y=\"2\" />");

            Assert.DoesNotContain(LevelValidator.Validate(doc), w => w.Key == "Validation.ResolutionTooSmall");
        }

        /// <summary>Several case variants of settings warn because only the first layer is authoritative.</summary>
        [Fact]
        public void DuplicateSettingsLayersWarnCaseInsensitively()
        {
            LevelDocument doc = LevelDocument.Parse("""
                <map>
                    <layer name="Settings">
                        <map width="320" height="480" />
                        <gameDesign twoParts="false" />
                    </layer>
                    <layer name="SETTINGS"><map width="999" height="999" /></layer>
                    <layer name="Objects"><candy x="1" y="1" /><target x="2" y="2" /></layer>
                </map>
                """);

            Assert.Contains(LevelValidator.Validate(doc),
                warning => warning.Key == "Validation.DuplicateSettingsLayer");
        }

        /// <summary>Duplicate candyNumber values produce a validation warning.</summary>
        [Fact]
        public void WarnsOnDuplicateCandyNumbers()
        {
            LevelDocument doc = Doc("twoParts=\"false\"",
                "<target x=\"1\" y=\"1\" /><candy x=\"1\" y=\"1\" candyNumber=\"0\" /><candy x=\"2\" y=\"2\" candyNumber=\"0\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.DuplicateCandyNumber");
        }

        /// <summary>Grabs with a candyNumber that no candy has produce a warning.</summary>
        [Fact]
        public void WarnsOnGrabWithUnmatchedCandyNumber()
        {
            LevelDocument doc = Doc("twoParts=\"false\"",
                "<target x=\"1\" y=\"1\" /><candy x=\"1\" y=\"1\" candyNumber=\"0\" /><grab x=\"3\" y=\"3\" length=\"10\" candyNumber=\"7\" />");

            Assert.Contains(LevelValidator.Validate(doc),
                w => w.Key == "Validation.GrabUnmatchedCandyNumber" && w.Args.Contains("7"));
        }

        /// <summary>Bulb-bound grabs warn when their bulbNumber has no matching bulb.</summary>
        [Fact]
        public void WarnsOnBindBulbWithNoMatchingBulb()
        {
            LevelDocument doc = Doc("twoParts=\"false\"",
                "<target x=\"1\" y=\"1\" /><candy x=\"1\" y=\"1\" candyNumber=\"0\" /><grab x=\"3\" y=\"3\" bindBulb=\"true\" bulbNumber=\"5\" />");

            Assert.Contains(LevelValidator.Validate(doc),
                w => w.Key == "Validation.GrabUnmatchedBulbNumber" && w.Args.Contains("5"));
        }

        /// <summary>A ghost with all morph states off warns that it does nothing.</summary>
        [Fact]
        public void IdleOnlyGhostWarns()
        {
            LevelDocument doc = Doc("",
                "<candy x=\"1\" y=\"1\" /><target x=\"3\" y=\"3\" />" +
                "<ghost x=\"5\" y=\"5\" grab=\"false\" bubble=\"false\" bouncer=\"false\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.GhostIdle");
        }

        /// <summary>A ghost with at least one state produces no idle warning.</summary>
        [Fact]
        public void GhostWithAStateHasNoIdleWarning()
        {
            LevelDocument doc = Doc("",
                "<candy x=\"1\" y=\"1\" /><target x=\"3\" y=\"3\" />" +
                "<ghost x=\"5\" y=\"5\" grab=\"true\" bubble=\"false\" bouncer=\"false\" />");

            Assert.DoesNotContain(LevelValidator.Validate(doc), w => w.Key == "Validation.GhostIdle");
        }

        /// <summary>A candy starting inside a spike is flagged so the author can move it.</summary>
        [Fact]
        public void CandyInsideSpikeWarns()
        {
            LevelDocument doc = Doc("",
                "<candy x=\"0\" y=\"0\" candyNumber=\"1\" /><spike1 x=\"0\" y=\"0\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.CandyInHazard");
        }

        /// <summary>A candy clear of every hazard produces no hazard warning.</summary>
        [Fact]
        public void CandyClearOfHazardsDoesNotWarn()
        {
            LevelDocument doc = Doc("",
                "<candy x=\"0\" y=\"0\" /><spike1 x=\"200\" y=\"200\" /><target x=\"200\" y=\"400\" />");

            Assert.DoesNotContain(LevelValidator.Validate(doc), w => w.Key == "Validation.CandyInHazard");
        }

        /// <summary>A candy starting on Om Nom's mouth is flagged so the author can move it.</summary>
        [Fact]
        public void CandyOnMouthWarns()
        {
            LevelDocument doc = Doc("",
                "<candy x=\"100\" y=\"100\" candyNumber=\"1\" /><target x=\"100\" y=\"100\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.CandyOnMouth");
        }

        /// <summary>A candy clear of every Om Nom's mouth produces no mouth warning.</summary>
        [Fact]
        public void CandyClearOfMouthDoesNotWarn()
        {
            LevelDocument doc = Doc("",
                "<candy x=\"100\" y=\"100\" /><target x=\"250\" y=\"400\" />");

            Assert.DoesNotContain(LevelValidator.Validate(doc), w => w.Key == "Validation.CandyOnMouth");
        }
        /// <summary>A hook sitting exactly above its candy makes a vertical rope, which the game mishandles.</summary>
        [Fact]
        public void GrabVerticallyAlignedWithItsCandyWarns()
        {
            LevelDocument doc = Doc("",
                "<candy x=\"240\" y=\"400\" /><grab x=\"240\" y=\"100\" length=\"200\" /><target x=\"300\" y=\"300\" />");

            Assert.Contains(
                LevelValidator.Validate(doc),
                w => w.Key == "Validation.GrabVerticallyAligned");
        }

        /// <summary>One unit of offset is enough; the warning is about the exactly-degenerate case only.</summary>
        [Fact]
        public void GrabOffsetFromItsCandyDoesNotWarn()
        {
            LevelDocument doc = Doc("",
                "<candy x=\"241\" y=\"400\" /><grab x=\"240\" y=\"100\" length=\"200\" /><target x=\"300\" y=\"300\" />");

            Assert.DoesNotContain(
                LevelValidator.Validate(doc),
                w => w.Key == "Validation.GrabVerticallyAligned");
        }

        /// <summary>Hooks that bind during play have no authored rope, so alignment cannot bite them.</summary>
        [Theory]
        [InlineData("gun=\"true\"")]
        [InlineData("radius=\"120\"")]
        public void GrabsWithoutAnAuthoredRopeDoNotWarnOnAlignment(string extra)
        {
            LevelDocument doc = Doc("",
                $"<candy x=\"240\" y=\"400\" /><grab x=\"240\" y=\"100\" length=\"200\" {extra} /><target x=\"300\" y=\"300\" />");

            Assert.DoesNotContain(
                LevelValidator.Validate(doc),
                w => w.Key == "Validation.GrabVerticallyAligned");
        }

        /// <summary>A rope bound to a light bulb is checked against the bulb it actually binds to.</summary>
        [Fact]
        public void GrabVerticallyAlignedWithItsBulbWarns()
        {
            LevelDocument doc = Doc("",
                "<candy x=\"10\" y=\"400\" /><lightBulb x=\"240\" y=\"400\" bulbNumber=\"0\" />"
                + "<grab x=\"240\" y=\"100\" length=\"200\" bindBulb=\"true\" bulbNumber=\"0\" />"
                + "<target x=\"300\" y=\"300\" />");

            Assert.Contains(
                LevelValidator.Validate(doc),
                w => w.Key == "Validation.GrabVerticallyAligned");
        }
    }
}
