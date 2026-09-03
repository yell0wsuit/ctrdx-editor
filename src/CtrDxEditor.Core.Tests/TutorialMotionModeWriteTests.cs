using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Switching motion mode writes the attribute combination that selects it.</summary>
    public class TutorialMotionModeWriteTests
    {
        private static LevelObject Prompt(params (string Name, string Value)[] attributes)
        {
            XElement element = new("tutorial10");
            foreach ((string attribute, string value) in attributes)
            {
                element.SetAttributeValue(attribute, value);
            }

            return new LevelObject(element);
        }

        /// <summary>None clears every motion attribute, including the inert speeds.</summary>
        [Fact]
        public void NoneClearsMotionAttributes()
        {
            LevelObject prompt = Prompt(
                ("path", "100,0"), ("ease", "in"), ("moveDelay", "1"), ("moveSpeed", "440"), ("rotateSpeed", "100"));

            TutorialMotionEditor.SetMode(prompt, TutorialMotionMode.None);

            Assert.Null(prompt.GetAttr("path"));
            Assert.Null(prompt.GetAttr("ease"));
            Assert.Null(prompt.GetAttr("moveDelay"));
            Assert.Null(prompt.GetAttr("moveSpeed"));
            Assert.Null(prompt.GetAttr("rotateSpeed"));
            Assert.Equal(TutorialMotionMode.None, TutorialMotion.ModeOf(prompt));
        }

        /// <summary>Looping keeps a path and speeds but drops the timeline-only attributes.</summary>
        [Fact]
        public void LoopingDropsTimelineAttributes()
        {
            LevelObject prompt = Prompt(("path", "100,0"), ("ease", "in"), ("moveDelay", "1"), ("repeat", "2"));

            TutorialMotionEditor.SetMode(prompt, TutorialMotionMode.Looping);

            Assert.Equal("100,0", prompt.GetAttr("path"));
            Assert.Null(prompt.GetAttr("ease"));
            Assert.Null(prompt.GetAttr("moveDelay"));
            Assert.Null(prompt.GetAttr("repeat"));
            Assert.Equal(TutorialMotionMode.Looping, TutorialMotion.ModeOf(prompt));
        }

        /// <summary>Timed drops rotateSpeed, which the timeline cannot express, and seeds an ease.</summary>
        [Fact]
        public void TimedDropsRotateSpeedAndSeedsEase()
        {
            LevelObject prompt = Prompt(("path", "100,0"), ("rotateSpeed", "100"));

            TutorialMotionEditor.SetMode(prompt, TutorialMotionMode.Timed);

            Assert.Null(prompt.GetAttr("rotateSpeed"));
            Assert.Equal("none", prompt.GetAttr("ease"));
            Assert.Equal(TutorialMotionMode.Timed, TutorialMotion.ModeOf(prompt));
        }

        /// <summary>A circular path cannot become timed motion, so the mode switch replaces it.</summary>
        [Fact]
        public void TimedReplacesACircularPath()
        {
            LevelObject prompt = Prompt(("path", "R50"));

            TutorialMotionEditor.SetMode(prompt, TutorialMotionMode.Timed);

            Assert.False(MoverPath.IsCircularPath(prompt.GetAttr("path")));
            Assert.NotNull(TutorialMotion.Timed(prompt));
        }

        /// <summary>Entering a motion mode from nothing seeds a usable default path.</summary>
        [Fact]
        public void EnteringAModeSeedsAPath()
        {
            LevelObject prompt = Prompt();

            TutorialMotionEditor.SetMode(prompt, TutorialMotionMode.Looping);

            Assert.NotNull(prompt.GetAttr("path"));
            Assert.Equal(TutorialMotionMode.Looping, TutorialMotion.ModeOf(prompt));
        }
    }
}
