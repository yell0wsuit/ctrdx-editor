using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>One rule per throw in the game's TutorialPromptLoader.Parse.</summary>
    public class TutorialValidationTests
    {
        private static LevelDocument Level(string objects, string gameDesign = "twoParts=\"false\"")
        {
            return LevelDocument.Parse($"""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="320" height="480" />
                        <gameDesign ropePhysicsSpeed="1.0" {gameDesign} />
                    </layer>
                    <layer name="Objects">{objects}</layer>
                </map>
                """);
        }

        private static string[] Keys(LevelDocument document)
        {
            return [.. TutorialValidation.Validate(document).Select(w => w.Key)];
        }

        /// <summary>A schema-valid prompt produces no findings.</summary>
        [Fact]
        public void ValidPromptProducesNothing()
        {
            Assert.Empty(Keys(Level("""<tutorialText x="10" y="10" locale="en" text="T" width="100" duration="10" />""")));
        }

        /// <summary>Each invalid schema condition is reported under its own localized key.</summary>
        [Theory]
        [InlineData("""<tutorial01 x="1" y="1" showOn="ropeCutt" />""", "Validation.Tutorial.UnknownEvent")]
        [InlineData("""<tutorial01 x="1" y="1" subject="both" />""", "Validation.Tutorial.UnknownSubject")]
        [InlineData("""<tutorial01 x="1" y="1" showOn="candyMoved" />""", "Validation.Tutorial.AreaRequired")]
        [InlineData("""<tutorial01 x="1" y="1" inArea="1,2,0,4" />""", "Validation.Tutorial.InvalidArea")]
        [InlineData("""<tutorial01 x="1" y="1" subject="left" />""", "Validation.Tutorial.SplitSubject")]
        [InlineData("""<tutorial01 x="1" y="1" size="1.4" />""", "Validation.Tutorial.TextOnlyAttribute")]
        [InlineData("""<tutorial01 x="1" y="1" anim="swipe" />""", "Validation.Tutorial.StaleAnimation")]
        [InlineData("""<tutorial01 x="1" y="1" duration="-1" repeat="2" />""", "Validation.Tutorial.RepeatWithForeverHold")]
        [InlineData("""<tutorial01 x="1" y="1" repeat="0" />""", "Validation.Tutorial.InvalidRepeat")]
        [InlineData("""<tutorial01 x="1" y="1" fadeIn="-2" />""", "Validation.Tutorial.InvalidTime")]
        [InlineData("""<tutorial01 x="1" y="1" duration="-0.5" />""", "Validation.Tutorial.InvalidTime")]
        [InlineData("""<tutorial01 x="1" y="1" opacity="1.5" />""", "Validation.Tutorial.InvalidOpacity")]
        [InlineData("""<tutorial01 x="1" y="1" angle="NaN" />""", "Validation.Tutorial.InvalidAngle")]
        [InlineData("""<tutorial01 x="1" y="1" moveSpeed="0" />""", "Validation.Tutorial.InvalidMultiplier")]
        [InlineData("""<tutorial01 x="1" y="1" color="not-a-color" />""", "Validation.Tutorial.InvalidColor")]
        [InlineData("""<tutorial10 x="1" y="1" color="#FF0000" />""", "Validation.Tutorial.ColoredQuad")]
        [InlineData("""<tutorial01 x="1" y="1" path="10,20,30" ease="in" />""", "Validation.Tutorial.InvalidPath")]
        [InlineData("""<tutorial01 x="1" y="1" path="10,0,20,0" ease="in,out,none" />""", "Validation.Tutorial.UnknownEase")]
        public void EachRuleReportsItsOwnError(string element, string expectedKey)
        {
            LevelWarning finding = Assert.Single(
                TutorialValidation.Validate(Level(element)), w => w.Key == expectedKey);

            Assert.Equal(LevelWarningSeverity.Error, finding.Severity);
        }

        /// <summary>Travel has to fit inside one pass, or the game refuses the motion.</summary>
        [Fact]
        public void TravelExceedingThePassIsAnError()
        {
            string element = """<tutorial01 x="1" y="1" path="1000,0" moveSpeed="10" ease="in" fadeIn="1" duration="1" fadeOut="0.5" />""";

            Assert.Contains("Validation.Tutorial.TravelExceedsPass", Keys(Level(element)));
        }

        /// <summary>Split subjects are legal once the level authors two parts.</summary>
        [Fact]
        public void SplitSubjectIsLegalInATwoPartLevel()
        {
            LevelDocument document = Level("""<tutorial01 x="1" y="1" subject="left" />""", "twoParts=\"true\"");

            Assert.DoesNotContain("Validation.Tutorial.SplitSubject", Keys(document));
        }

        /// <summary>A dead special is inert, so it is a warning rather than an error.</summary>
        [Fact]
        public void DeadSpecialIsAWarning()
        {
            LevelWarning finding = Assert.Single(
                TutorialValidation.Validate(Level("""<tutorial01 x="1" y="1" special="2" />""")),
                w => w.Key == "Validation.Tutorial.DeadSpecial");

            Assert.Equal(LevelWarningSeverity.Warning, finding.Severity);
        }

        /// <summary>Every locale copy is checked, because a broken copy costs that language its prompt.</summary>
        [Fact]
        public void NonEnglishLocaleCopiesAreValidated()
        {
            LevelDocument document = LevelDocument.Parse("""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="320" height="480" />
                        <gameDesign ropePhysicsSpeed="1.0" twoParts="false" />
                    </layer>
                    <layer name="Objects"><tutorial01 x="1" y="1" locale="en" /></layer>
                    <layer name="Ru"><tutorial01 x="1" y="1" locale="ru" showOn="nope" /></layer>
                </map>
                """);

            LevelWarning finding = Assert.Single(
                TutorialValidation.Validate(document), w => w.Key == "Validation.Tutorial.UnknownEvent");

            Assert.Equal("ru", finding.Args[0]);
        }

        /// <summary>Findings name the locale and element so they match the game's stderr line.</summary>
        [Fact]
        public void FindingsNameLocaleAndElement()
        {
            LevelWarning finding = Assert.Single(
                TutorialValidation.Validate(Level("""<tutorial04 x="1" y="1" anim="swipe" />""")));

            Assert.Equal(["en", "tutorial04"], finding.Args);
        }
    }
}
