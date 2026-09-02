using System.IO;
using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// The editor must agree with the game about the shipped tutorial maps. If DX changes the
    /// schema again, this is the test that says so.
    /// </summary>
    public class TutorialContentTests
    {
        /// <summary>Every selected migrated map remains free of schema errors.</summary>
        [Theory]
        [InlineData("TestData/tutorial_1_1.xml")]
        [InlineData("TestData/tutorial_1_5.xml")]
        [InlineData("TestData/tutorial_14_1.xml")]
        [InlineData("TestData/tutorial_15_1.xml")]
        [InlineData("TestData/tutorial_17_1.xml")]
        public void ShippedTutorialMapsValidateClean(string path)
        {
            LevelDocument document = LevelDocument.Parse(File.ReadAllText(path));

            LevelWarning[] errors = [.. TutorialValidation.Validate(document)
                .Where(w => w.Severity == LevelWarningSeverity.Error)];

            Assert.Empty(errors);
        }

        /// <summary>1_1's swiping hand is the worked example of authored timed motion.</summary>
        [Fact]
        public void SwipingHandParsesAsTimedMotion()
        {
            LevelDocument document = LevelDocument.Parse(File.ReadAllText("TestData/tutorial_1_1.xml"));
            LevelObject hand = document.AllObjects.First(
                o => o.Type == "tutorial10" && o.GetAttr("locale") == "en");

            Assert.Equal(TutorialMotionMode.Timed, TutorialMotion.ModeOf(hand));

            TutorialMotion motion = TutorialMotion.Timed(hand)!;
            Assert.Equal([TutorialEase.In, TutorialEase.Out], motion.Eases);
            Assert.Equal(1.5, motion.MoveDelay, 3);

            TutorialTiming timing = TutorialTiming.For(hand);
            Assert.Equal(2, timing.Repeat);
            Assert.Equal(3.1, timing.PassSeconds, 3);
            Assert.True(motion.TravelSeconds <= timing.PassSeconds);
        }

        /// <summary>Static prompts keep inert speeds from the old schema and must read as motionless.</summary>
        [Fact]
        public void StaticPromptsWithInertSpeedsReadAsNoMotion()
        {
            LevelDocument document = LevelDocument.Parse(File.ReadAllText("TestData/tutorial_1_1.xml"));
            LevelObject sign = document.AllObjects.First(
                o => o.Type == "tutorial01" && o.GetAttr("locale") == "en");

            Assert.Equal("100", sign.GetAttr("moveSpeed"));
            Assert.Null(sign.GetAttr("path"));
            Assert.Equal(TutorialMotionMode.None, TutorialMotion.ModeOf(sign));
        }
    }
}
