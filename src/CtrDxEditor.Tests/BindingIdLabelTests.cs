using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the binding id labels drawn on candies, light bulbs and grouped hats.</summary>
    public class BindingIdLabelTests
    {
        /// <summary>A lone candy binds unambiguously, so it carries no label.</summary>
        [Fact]
        public void SingleCandyShowsNoLabel()
        {
            LevelObject[] objects = Parse("""<candy x="1" y="1" />""", """<star x="2" y="2" />""");

            Assert.Null(BindingIdLabel(objects[0], objects));
        }

        /// <summary>Candies in company show their candyNumber, or their position among candies when unnumbered.</summary>
        [Fact]
        public void CandiesShowTheirNumberOrPosition()
        {
            LevelObject[] objects = Parse(
                """<candy x="1" y="1" />""",
                """<star x="2" y="2" />""",
                """<candy x="3" y="3" candyNumber="7" />""",
                """<candy x="4" y="4" candyNumber="" />""");

            Assert.Equal("0", BindingIdLabel(objects[0], objects));
            Assert.Null(BindingIdLabel(objects[1], objects));
            Assert.Equal("7", BindingIdLabel(objects[2], objects));
            Assert.Equal("2", BindingIdLabel(objects[3], objects));
        }

        /// <summary>Bulbs label by bulbNumber, grouped only with bulbs spelled the same way.</summary>
        [Fact]
        public void BulbsLabelWithinTheirOwnSpelling()
        {
            LevelObject[] objects = Parse(
                """<lightBulb x="1" y="1" bulbNumber="4" />""",
                """<lightBulb x="2" y="2" />""",
                """<lightbulb x="3" y="3" />""");

            Assert.Equal("4", BindingIdLabel(objects[0], objects));
            Assert.Equal("1", BindingIdLabel(objects[1], objects));
            Assert.Null(BindingIdLabel(objects[2], objects));
        }

        /// <summary>Grouped hats are labeled once two distinct nonzero groups exist; plain hats never are.</summary>
        [Fact]
        public void HatsLabelOnlyWhenGroupsAreAmbiguous()
        {
            LevelObject[] oneGroup = Parse("""<sock x="1" y="1" group="1" />""", """<sock x="2" y="2" group="1" />""");
            LevelObject[] twoGroups = Parse(
                """<sock x="1" y="1" group="1" />""",
                """<sock x="2" y="2" group="2" />""",
                """<sock x="3" y="3" group="0" />""");

            Assert.Null(BindingIdLabel(oneGroup[0], oneGroup));
            Assert.Equal("1", BindingIdLabel(twoGroups[0], twoGroups));
            Assert.Equal("2", BindingIdLabel(twoGroups[1], twoGroups));
            Assert.Null(BindingIdLabel(twoGroups[2], twoGroups));
        }

        private static LevelObject[] Parse(params string[] elements)
        {
            return [.. elements.Select(e => new LevelObject(XElement.Parse(e)))];
        }

        private static string? BindingIdLabel(LevelObject obj, IReadOnlyList<LevelObject> objects)
        {
            Type renderer = typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;
            MethodInfo method = renderer.GetMethod("BindingIdLabel", BindingFlags.NonPublic | BindingFlags.Static)!;
            return (string?)method.Invoke(null, [obj, objects]);
        }
    }
}
