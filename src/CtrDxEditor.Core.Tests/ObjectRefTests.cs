using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for structural object coordinates across level layers.</summary>
    public class ObjectRefTests
    {
        private static LevelDocument Doc()
        {
            return LevelDocument.Parse("""
            <?xml version='1.0' encoding='utf-8'?>
            <map>
                <layer name="settings"><map width="320" height="480" /></layer>
                <layer name="a"><candy x="1" y="2" /></layer>
                <layer name="b"><star x="3" y="4" timeout="-1" /><bubble x="5" y="6" /></layer>
            </map>
            """);
        }

        /// <summary>Verifies that an object in any layer round-trips through its structural coordinate.</summary>
        [Fact]
        public void RefOfThenResolveRoundTripsInAnyLayer()
        {
            LevelDocument doc = Doc();
            LevelObject bubble = doc.Layers[1].Objects[1];

            ObjectRef? reference = doc.RefOf(bubble);

            Assert.Equal(new ObjectRef(1, 1), reference);
            Assert.Equal(bubble, doc.Resolve(reference!.Value));
        }

        /// <summary>Verifies that invalid layer and object indices resolve to null.</summary>
        [Fact]
        public void ResolveOutOfRangeReturnsNull()
        {
            LevelDocument doc = Doc();

            Assert.Null(doc.Resolve(new ObjectRef(9, 0)));
            Assert.Null(doc.Resolve(new ObjectRef(0, 9)));
        }
    }
}
