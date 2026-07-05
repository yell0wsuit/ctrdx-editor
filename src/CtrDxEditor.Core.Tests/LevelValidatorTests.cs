using System.Collections.Generic;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the non-blocking level validation rules.</summary>
    public class LevelValidatorTests
    {
        private static LevelDocument Doc(string settingsFlags, string objects) => LevelDocument.Parse($"""
            <map>
                <layer name="settings">
                    <map gridSize="32" width="320" height="480" />
                    <gameDesign {settingsFlags} />
                </layer>
                <layer name="Objects">{objects}</layer>
            </map>
            """);

        [Fact]
        public void ValidTwoPartLevelHasNoWarnings()
        {
            LevelDocument doc = Doc("twoParts=\"true\"",
                "<candyL x=\"1\" y=\"1\" /><candyR x=\"2\" y=\"2\" /><target x=\"3\" y=\"3\" />");

            Assert.Empty(LevelValidator.Validate(doc));
        }

        [Fact]
        public void TwoPartMissingHalfWarns()
        {
            LevelDocument doc = Doc("twoParts=\"true\"",
                "<candyL x=\"1\" y=\"1\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("candy half"));
        }

        [Fact]
        public void PlainCandyInTwoPartLevelWarns()
        {
            LevelDocument doc = Doc("twoParts=\"true\"",
                "<candyL x=\"1\" y=\"1\" /><candyR x=\"2\" y=\"2\" /><candy x=\"9\" y=\"9\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("plain candy"));
        }

        [Fact]
        public void SplitCandyInSingleLevelWarns()
        {
            LevelDocument doc = Doc("twoParts=\"false\"",
                "<candy x=\"1\" y=\"1\" /><candyL x=\"5\" y=\"5\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("candyL/candyR"));
        }

        [Fact]
        public void NightLevelWithoutBulbWarns()
        {
            LevelDocument doc = Doc("nightLevel=\"true\"",
                "<candy x=\"1\" y=\"1\" /><target x=\"3\" y=\"3\" />");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Contains("light bulb"));
        }

        [Fact]
        public void MissingCandyAndTargetWarn()
        {
            LevelDocument doc = Doc("twoParts=\"false\"", "");

            IReadOnlyList<string> warnings = LevelValidator.Validate(doc);
            Assert.Contains(warnings, w => w.Contains("no candy"));
            Assert.Contains(warnings, w => w.Contains("Om Nom"));
        }

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

        [Fact]
        public void NormalResolutionDoesNotWarnAboutSize()
        {
            LevelDocument doc = Doc("twoParts=\"false\"", "<candy x=\"1\" y=\"1\" /><target x=\"2\" y=\"2\" />");

            Assert.DoesNotContain(LevelValidator.Validate(doc), w => w.Contains("smaller than"));
        }
    }
}
