using System.Xml.Linq;

using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    public class LevelObjectTests
    {
        [Fact]
        public void SettingXWritesBackToTheAttribute()
        {
            LevelObject obj = new(XElement.Parse("""<star x="10" y="20" timeout="-1" />"""))
            {
                X = 42
            };

            Assert.Equal(42, obj.X);
            Assert.Equal("42", obj.Element.Attribute("x")!.Value);
            Assert.Equal("star", obj.Type);
            Assert.Equal("-1", obj.GetAttr("timeout"));
        }
    }
}
