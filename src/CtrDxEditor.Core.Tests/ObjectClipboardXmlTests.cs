using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the text form objects take on the system clipboard, and what comes back.</summary>
    public class ObjectClipboardXmlTests
    {
        private static readonly DescriptorTable Descriptors = DescriptorTable.CtrObjects;

        /// <summary>Written objects come back as the same elements.</summary>
        [Fact]
        public void WriteThenReadRoundTripsEveryObject()
        {
            XElement[] source =
            [
                XElement.Parse("<bubble x=\"512\" y=\"300\" />"),
                XElement.Parse("<star x=\"10\" y=\"20\" />"),
            ];

            ClipboardXmlResult result = ObjectClipboardXml.Read(ObjectClipboardXml.Write(source), Descriptors);

            Assert.Equal(ClipboardXmlOutcome.Parsed, result.Outcome);
            Assert.Equal(["bubble", "star"], result.Elements.Select(e => e.Name.LocalName));
            Assert.Equal("512", result.Elements[0].Attribute("x")!.Value);
        }

        /// <summary>Two sibling elements are not a well-formed document, so a wrapper is added on read.</summary>
        [Fact]
        public void WriteEmitsBareSiblingsWithNoWrapper()
        {
            string text = ObjectClipboardXml.Write([XElement.Parse("<bubble x=\"1\" y=\"2\" />")]);

            Assert.StartsWith("<bubble", text, System.StringComparison.Ordinal);
            Assert.DoesNotContain("<clipboard", text, System.StringComparison.Ordinal);
        }

        /// <summary>A chunk lifted out of a level file pastes, wrapper and all.</summary>
        [Theory]
        [InlineData("<layer name=\"Objects\"><bubble x=\"1\" y=\"2\" /></layer>")]
        [InlineData("<map><layer name=\"Objects\"><bubble x=\"1\" y=\"2\" /></layer></map>")]
        [InlineData("<?xml version=\"1.0\" encoding=\"utf-8\"?><map><layer name=\"L0\"><bubble x=\"1\" y=\"2\" /></layer></map>")]
        public void ContainersAreUnwrappedAtAnyDepth(string text)
        {
            ClipboardXmlResult result = ObjectClipboardXml.Read(text, Descriptors);

            Assert.Equal(ClipboardXmlOutcome.Parsed, result.Outcome);
            Assert.Equal("bubble", Assert.Single(result.Elements).Name.LocalName);
        }

        /// <summary>Unknown element names are dropped rather than becoming objects.</summary>
        [Fact]
        public void UnknownElementsAreSkipped()
        {
            ClipboardXmlResult result = ObjectClipboardXml.Read(
                "<bubble x=\"1\" y=\"2\" /><nonsense a=\"b\" />",
                Descriptors);

            Assert.Equal(ClipboardXmlOutcome.Parsed, result.Outcome);
            Assert.Equal("bubble", Assert.Single(result.Elements).Name.LocalName);
        }

        /// <summary>A namespaced element matching only on local name is not ours.</summary>
        [Fact]
        public void NamespacedElementsAreRejected()
        {
            ClipboardXmlResult result = ObjectClipboardXml.Read(
                "<html:bubble xmlns:html=\"http://www.w3.org/1999/xhtml\" x=\"1\" y=\"2\" />",
                Descriptors);

            Assert.Equal(ClipboardXmlOutcome.NotOurs, result.Outcome);
            Assert.Empty(result.Elements);
        }

        /// <summary>Ordinary text is not a failed paste and must not raise anything.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("just some prose a user copied")]
        [InlineData("<div class=\"x\">markup from a web page</div>")]
        public void UnrelatedTextIsNotOurs(string? text)
        {
            ClipboardXmlResult result = ObjectClipboardXml.Read(text, Descriptors);

            Assert.Equal(ClipboardXmlOutcome.NotOurs, result.Outcome);
            Assert.Empty(result.Elements);
        }

        /// <summary>Text that names an object but will not parse is a failed paste, not noise.</summary>
        [Fact]
        public void MalformedTextThatNamesAnObjectIsRejected()
        {
            ClipboardXmlResult result = ObjectClipboardXml.Read("<bubble x=\"1\" y=\"2\"", Descriptors);

            Assert.Equal(ClipboardXmlOutcome.Rejected, result.Outcome);
            Assert.Empty(result.Elements);
        }

        /// <summary>A DTD is refused by the reader, which lands in the same rejection path.</summary>
        [Fact]
        public void DtdPayloadsAreRejected()
        {
            ClipboardXmlResult result = ObjectClipboardXml.Read(
                "<!DOCTYPE bubble [<!ENTITY a \"aaaaaaaaaa\">]><bubble x=\"1\" y=\"&a;\" />",
                Descriptors);

            Assert.Equal(ClipboardXmlOutcome.Rejected, result.Outcome);
        }

        /// <summary>Oversized text is refused before parsing and stays silent.</summary>
        [Fact]
        public void TextOverTheCapIsNotOursAndIsNeverParsed()
        {
            string huge = "<bubble x=\"1\" y=\"2\" />" + new string(' ', ObjectClipboardXml.MaxTextLength);

            ClipboardXmlResult result = ObjectClipboardXml.Read(huge, Descriptors);

            Assert.Equal(ClipboardXmlOutcome.NotOurs, result.Outcome);
        }

        /// <summary>Returned elements are detached copies the caller can mutate freely.</summary>
        [Fact]
        public void ReadReturnsDetachedCopies()
        {
            ClipboardXmlResult result = ObjectClipboardXml.Read("<bubble x=\"1\" y=\"2\" />", Descriptors);

            XElement pasted = Assert.Single(result.Elements);
            Assert.Null(pasted.Parent);
        }
    }
}
