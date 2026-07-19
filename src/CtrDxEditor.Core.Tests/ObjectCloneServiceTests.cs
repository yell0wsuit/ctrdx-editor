using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests cloning level objects and remapping their bindings.</summary>
    public class ObjectCloneServiceTests
    {
        private static LevelDocument Doc(string layerBody)
        {
            return LevelDocument.Parse(
                "<map><layer name=\"settings\"><map/></layer>" +
                "<layer name=\"L0\">" + layerBody + "</layer></map>");
        }

        /// <summary>Verifies cloning deep-copies an object and appends it to the target layer.</summary>
        [Fact]
        public void CloneDeepCopiesAndAppendsToTarget()
        {
            LevelDocument doc = Doc("<bubble x=\"5\" y=\"6\"/>");
            LevelLayer target = doc.Layers[0];
            LevelObject source = target.Objects[0];

            IReadOnlyList<LevelObject> clones = ObjectCloneService.Clone([source], target, doc);

            _ = Assert.Single(clones);
            Assert.NotSame(source.Element, clones[0].Element);
            Assert.Equal(2, target.Objects.Count);
            Assert.Equal("5", clones[0].GetAttr("x"));
        }

        /// <summary>Verifies cloning skips object types already at placement capacity.</summary>
        [Fact]
        public void CloneSkipsTypesAtCapacity()
        {
            LevelDocument doc = Doc("<candyL x=\"1\" y=\"1\"/>");
            LevelLayer target = doc.Layers[0];
            LevelObject candyLeft = target.Objects[0];

            IReadOnlyList<LevelObject> clones = ObjectCloneService.Clone([candyLeft], target, doc);

            Assert.Empty(clones);
            _ = Assert.Single(target.Objects);
        }

        /// <summary>Verifies cloning candy assigns a fresh candy number.</summary>
        [Fact]
        public void CloneCandyGetsFreshNumber()
        {
            LevelDocument doc = Doc("<candy x=\"1\" y=\"1\" candyNumber=\"0\"/>");
            LevelLayer target = doc.Layers[0];
            LevelObject candy = target.Objects[0];

            IReadOnlyList<LevelObject> clones = ObjectCloneService.Clone([candy], target, doc);

            _ = Assert.Single(clones);
            Assert.NotEqual("0", clones[0].GetAttr("candyNumber"));
        }

        /// <summary>Verifies co-cloned grabs bind to their cloned candy.</summary>
        [Fact]
        public void CloneGrabWithItsCandyRemapsBindingToTheClone()
        {
            LevelDocument doc = Doc(
                "<candy x=\"1\" y=\"1\" candyNumber=\"0\"/>" +
                "<grab x=\"2\" y=\"2\" candyNumber=\"0\"/>");
            LevelLayer target = doc.Layers[0];
            LevelObject candy = target.Objects[0];
            LevelObject grab = target.Objects[1];

            IReadOnlyList<LevelObject> clones = ObjectCloneService.Clone([candy, grab], target, doc);
            LevelObject candyClone = clones.First(o => o.Type == "candy");
            LevelObject grabClone = clones.First(o => o.Type == "grab");

            Assert.Equal(candyClone.GetAttr("candyNumber"), grabClone.GetAttr("candyNumber"));
            Assert.NotEqual("0", grabClone.GetAttr("candyNumber"));
        }

        /// <summary>Verifies a cloned grab keeps its original binding when its candy is not cloned.</summary>
        [Fact]
        public void CloneGrabWithoutItsCandyKeepsOriginalBinding()
        {
            LevelDocument doc = Doc(
                "<candy x=\"1\" y=\"1\" candyNumber=\"0\"/>" +
                "<grab x=\"2\" y=\"2\" candyNumber=\"0\"/>");
            LevelLayer target = doc.Layers[0];
            LevelObject grab = target.Objects[1];

            IReadOnlyList<LevelObject> clones = ObjectCloneService.Clone([grab], target, doc);

            Assert.Equal("0", clones[0].GetAttr("candyNumber"));
        }
    }
}
