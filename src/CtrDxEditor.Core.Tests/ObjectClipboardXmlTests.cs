using System;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the text form objects take on their way out to the system clipboard.</summary>
    public class ObjectClipboardXmlTests
    {
        /// <summary>An object goes out as the exact line that belongs in a level file.</summary>
        [Fact]
        public void WriteEmitsBareSiblingsWithNoWrapper()
        {
            string text = ObjectClipboardXml.Write([XElement.Parse("<bubble x=\"1\" y=\"2\" />")]);

            Assert.StartsWith("<bubble", text, StringComparison.Ordinal);
            Assert.DoesNotContain("<clipboard", text, StringComparison.Ordinal);
        }

        /// <summary>Several objects keep their order, one per line.</summary>
        [Fact]
        public void WriteKeepsEveryObjectInOrderOnItsOwnLine()
        {
            string text = ObjectClipboardXml.Write(
            [
                XElement.Parse("<bubble x=\"512\" y=\"300\" />"),
                XElement.Parse("<star x=\"10\" y=\"20\" />"),
            ]);

            Assert.Equal(
                ["<bubble x=\"512\" y=\"300\" />", "<star x=\"10\" y=\"20\" />"],
                text.Split('\n'));
        }

        /// <summary>An empty selection produces empty text rather than a stray separator.</summary>
        [Fact]
        public void WriteOfNothingIsEmpty()
        {
            Assert.Equal(string.Empty, ObjectClipboardXml.Write([]));
        }
    }
}
