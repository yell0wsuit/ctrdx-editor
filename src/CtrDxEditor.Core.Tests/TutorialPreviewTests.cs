using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tutorial prompts animate in preview; everything fires at zero.</summary>
    public class TutorialPreviewTests
    {
        private static LevelObject Prompt(string name, params (string Name, string Value)[] attributes)
        {
            XElement element = new(name);
            foreach ((string attribute, string value) in attributes)
            {
                element.SetAttributeValue(attribute, value);
            }

            return new LevelObject(element);
        }

        /// <summary>A prompt fades by default, so every prompt is previewable.</summary>
        [Fact]
        public void PromptsArePreviewable()
        {
            Assert.True(AnimationPreviewPolicy.CanPreview(Prompt("tutorialText")));
            Assert.True(AnimationPreviewPolicy.CanPreview(Prompt("tutorial01")));
        }

        /// <summary>The scrubber runs the longest finite prompt, clamped so one long hold cannot ruin it.</summary>
        [Fact]
        public void PreviewLengthIsTheLongestFinitePassClamped()
        {
            LevelDocument document = LevelDocument.Parse("""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="320" height="480" />
                        <gameDesign ropePhysicsSpeed="1.0" twoParts="false" />
                    </layer>
                    <layer name="Objects">
                        <tutorialText x="1" y="1" fadeIn="1" duration="5" fadeOut="0.5" />
                        <tutorialText x="2" y="2" fadeIn="1" duration="5" fadeOut="0.5" repeat="2" />
                    </layer>
                </map>
                """);

            Assert.Equal(13.0, AnimationPreviewPolicy.TutorialPreviewSeconds(document)!.Value, 3);
        }

        /// <summary>A forever hold contributes no finite length, so it cannot set the scrubber's duration.</summary>
        [Fact]
        public void ForeverPromptsDoNotSetThePreviewLength()
        {
            LevelDocument document = LevelDocument.Parse("""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="320" height="480" />
                        <gameDesign ropePhysicsSpeed="1.0" twoParts="false" />
                    </layer>
                    <layer name="Objects">
                        <tutorialText x="1" y="1" duration="-1" />
                    </layer>
                </map>
                """);

            Assert.Null(AnimationPreviewPolicy.TutorialPreviewSeconds(document));
        }

        /// <summary>A 600-second hold is clamped rather than making the scrubber useless.</summary>
        [Fact]
        public void LongHoldsAreClampedToSixtySeconds()
        {
            LevelDocument document = LevelDocument.Parse("""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="320" height="480" />
                        <gameDesign ropePhysicsSpeed="1.0" twoParts="false" />
                    </layer>
                    <layer name="Objects">
                        <tutorialText x="1" y="1" duration="600" />
                    </layer>
                </map>
                """);

            Assert.Equal(60.0, AnimationPreviewPolicy.TutorialPreviewSeconds(document)!.Value, 3);
        }
    }
}
