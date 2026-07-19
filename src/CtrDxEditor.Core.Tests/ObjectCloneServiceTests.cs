using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    public class ObjectCloneServiceTests
    {
        private static LevelDocument Doc(string layerBody)
        {
            return LevelDocument.Parse(
                "<map><layer name=\"settings\"><map/></layer>" +
                "<layer name=\"L0\">" + layerBody + "</layer></map>");
        }

        [Fact]
        public void Clone_deep_copies_and_appends_to_target()
        {
            LevelDocument doc = Doc("<bubble x=\"5\" y=\"6\"/>");
            LevelLayer target = doc.Layers[0];
            LevelObject source = target.Objects[0];

            var clones = ObjectCloneService.Clone(new[] { source }, target, doc);

            Assert.Single(clones);
            Assert.NotSame(source.Element, clones[0].Element);
            Assert.Equal(2, target.Objects.Count);
            Assert.Equal("5", clones[0].GetAttr("x"));
        }

        [Fact]
        public void Clone_skips_types_at_capacity()
        {
            LevelDocument doc = Doc("<candyL x=\"1\" y=\"1\"/>");
            LevelLayer target = doc.Layers[0];
            LevelObject candyLeft = target.Objects[0];

            var clones = ObjectCloneService.Clone(new[] { candyLeft }, target, doc);

            Assert.Empty(clones);
            Assert.Single(target.Objects);
        }

        [Fact]
        public void Clone_candy_gets_fresh_number()
        {
            LevelDocument doc = Doc("<candy x=\"1\" y=\"1\" candyNumber=\"0\"/>");
            LevelLayer target = doc.Layers[0];
            LevelObject candy = target.Objects[0];

            var clones = ObjectCloneService.Clone(new[] { candy }, target, doc);

            Assert.Single(clones);
            Assert.NotEqual("0", clones[0].GetAttr("candyNumber"));
        }

        [Fact]
        public void Clone_grab_with_its_candy_remaps_binding_to_the_clone()
        {
            LevelDocument doc = Doc(
                "<candy x=\"1\" y=\"1\" candyNumber=\"0\"/>" +
                "<grab x=\"2\" y=\"2\" candyNumber=\"0\"/>");
            LevelLayer target = doc.Layers[0];
            LevelObject candy = target.Objects[0];
            LevelObject grab = target.Objects[1];

            var clones = ObjectCloneService.Clone(new[] { candy, grab }, target, doc);
            LevelObject candyClone = clones.First(o => o.Type == "candy");
            LevelObject grabClone = clones.First(o => o.Type == "grab");

            Assert.Equal(candyClone.GetAttr("candyNumber"), grabClone.GetAttr("candyNumber"));
            Assert.NotEqual("0", grabClone.GetAttr("candyNumber"));
        }

        [Fact]
        public void Clone_grab_without_its_candy_keeps_original_binding()
        {
            LevelDocument doc = Doc(
                "<candy x=\"1\" y=\"1\" candyNumber=\"0\"/>" +
                "<grab x=\"2\" y=\"2\" candyNumber=\"0\"/>");
            LevelLayer target = doc.Layers[0];
            LevelObject grab = target.Objects[1];

            var clones = ObjectCloneService.Clone(new[] { grab }, target, doc);

            Assert.Equal("0", clones[0].GetAttr("candyNumber"));
        }
    }
}
