using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>The tutorial descriptors exist with the game's editable attributes.</summary>
    public class TutorialDescriptorTests
    {
        /// <summary>Registers every tutorial icon tag.</summary>
        [Fact]
        public void KnowsAllElevenIconTags()
        {
            for (int q = 0; q < TutorialObject.IconCount; q++)
            {
                Assert.True(DescriptorTable.CtrObjects.Knows(TutorialObject.TagForQuad(q)));
            }
        }

        /// <summary>Exposes the icon angle as a numeric attribute.</summary>
        [Fact]
        public void IconHasAngleAttribute()
        {
            ObjectDescriptor d = DescriptorTable.CtrObjects.For("tutorial01")!;
            Assert.Contains(d.Attributes, a => a.Name == "angle" && a.Type == AttrType.Number);
        }

        /// <summary>Uses one localization name for all icon variants.</summary>
        [Fact]
        public void IconsShareTutorialLocalizationName()
        {
            Assert.Equal("tutorial", DescriptorTable.CtrObjects.For("tutorial07")!.LocalizationName);
        }

        /// <summary>Exposes literal text and wrap width attributes for tutorial text.</summary>
        [Fact]
        public void TextHasTextAndWidthAttributes()
        {
            ObjectDescriptor d = DescriptorTable.CtrObjects.For("tutorialText")!;
            Assert.Contains(d.Attributes, a => a.Name == "text" && a.Type == AttrType.Text);
            Assert.Contains(d.Attributes, a => a.Name == "width" && a.Type == AttrType.Whole);
        }

        /// <summary>Rotates every tutorial icon using its authored angle without an offset.</summary>
        [Fact]
        public void AllIconsRotateWithZeroOffset()
        {
            for (int q = 0; q < TutorialObject.IconCount; q++)
            {
                RotationSpec? spec = RotationTable.For(TutorialObject.TagForQuad(q));
                Assert.NotNull(spec);
                Assert.Equal(0, spec!.DisplayOffset);
            }
        }

        /// <summary>Does not expose rotation for tutorial text.</summary>
        [Fact]
        public void TutorialTextDoesNotRotate()
        {
            Assert.Null(RotationTable.For("tutorialText"));
        }
    }
}
