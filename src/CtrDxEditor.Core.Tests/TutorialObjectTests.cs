using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tutorial tag/quad mapping, icon rename, colored-quad and invert rules, locale default.</summary>
    public class TutorialObjectTests
    {
        private static LevelObject Obj(string name, params (string, string)[] attrs)
        {
            XElement e = new(name);
            foreach ((string k, string v) in attrs)
            {
                e.SetAttributeValue(k, v);
            }

            return new LevelObject(e);
        }

        /// <summary>Maps the first tutorial tag to the first quad.</summary>
        [Fact]
        public void Tag01IsQuadZero()
        {
            Assert.Equal(0, TutorialObject.QuadForTag("tutorial01"));
        }

        /// <summary>Maps the final tutorial tag to the final quad.</summary>
        [Fact]
        public void Tag11IsQuadTen()
        {
            Assert.Equal(10, TutorialObject.QuadForTag("tutorial11"));
        }

        /// <summary>Rejects the tutorial text tag as an image tag.</summary>
        [Fact]
        public void NonImageTagIsMinusOne()
        {
            Assert.Equal(-1, TutorialObject.QuadForTag("tutorialText"));
        }

        /// <summary>Maps the final quad to the final tutorial tag.</summary>
        [Fact]
        public void QuadTenTagIsTutorial11()
        {
            Assert.Equal("tutorial11", TutorialObject.TagForQuad(10));
        }

        /// <summary>Recognizes exactly the supported tutorial image tags.</summary>
        [Fact]
        public void IsImageMatchesElevenTagsOnly()
        {
            Assert.True(TutorialObject.IsImage("tutorial01"));
            Assert.True(TutorialObject.IsImage("tutorial11"));
            Assert.False(TutorialObject.IsImage("tutorial12"));
            Assert.False(TutorialObject.IsImage("tutorialText"));
        }

        /// <summary>Recognizes only the tutorial text tag as text.</summary>
        [Fact]
        public void IsTextMatchesTutorialText()
        {
            Assert.True(TutorialObject.IsText("tutorialText"));
            Assert.False(TutorialObject.IsText("tutorial01"));
        }

        /// <summary>Renames an icon without discarding its attributes.</summary>
        [Fact]
        public void SetIconRenamesElementAndKeepsAttributes()
        {
            LevelObject o = Obj("tutorial04", ("x", "10"), ("y", "20"), ("angle", "35"));
            TutorialObject.SetIcon(o, 6);
            Assert.Equal("tutorial07", o.Type);
            Assert.Equal("10", o.GetAttr("x"));
            Assert.Equal("35", o.GetAttr("angle"));
            Assert.Equal(6, TutorialObject.Icon(o));
        }

        /// <summary>Marks only the finger icon quads as full-color.</summary>
        [Fact]
        public void OnlyQuads9And10AreColored()
        {
            Assert.True(TutorialObject.IsColoredQuad(9));
            Assert.True(TutorialObject.IsColoredQuad(10));
            Assert.False(TutorialObject.IsColoredQuad(0));
            Assert.False(TutorialObject.IsColoredQuad(8));
        }

        /// <summary>Inverts only line-art icons on a dark canvas.</summary>
        [Fact]
        public void ShouldInvertOnlyWhenDarkAndNotColored()
        {
            Assert.True(TutorialObject.ShouldInvert(0, dark: true));
            Assert.False(TutorialObject.ShouldInvert(0, dark: false));
            Assert.False(TutorialObject.ShouldInvert(9, dark: true));
            Assert.False(TutorialObject.ShouldInvert(10, dark: true));
        }

        /// <summary>Adds the English locale only when no locale exists.</summary>
        [Fact]
        public void EnsureEnglishLocaleSetsWhenAbsentAndKeepsExisting()
        {
            LevelObject a = Obj("tutorial01");
            TutorialObject.EnsureEnglishLocale(a);
            Assert.Equal("en", a.GetAttr("locale"));

            LevelObject b = Obj("tutorial01", ("locale", "ru"));
            TutorialObject.EnsureEnglishLocale(b);
            Assert.Equal("ru", b.GetAttr("locale"));
        }
    }
}
