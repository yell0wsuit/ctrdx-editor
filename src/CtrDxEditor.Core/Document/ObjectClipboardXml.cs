using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;

namespace CtrDxEditor.Core.Document
{
    /// <summary>What the clipboard text turned out to be.</summary>
    public enum ClipboardXmlOutcome
    {
        /// <summary>Not a paste of ours: empty, prose, unrelated markup, or too large. Stay silent.</summary>
        NotOurs,

        /// <summary>Parsed to at least one object.</summary>
        Parsed,

        /// <summary>Named an object we know but could not be used. Worth telling the user about.</summary>
        Rejected,
    }

    /// <summary>The outcome of reading clipboard text, and the objects it yielded.</summary>
    /// <param name="Outcome">What the text turned out to be.</param>
    /// <param name="Elements">Detached copies of the accepted objects; empty unless <see cref="ClipboardXmlOutcome.Parsed"/>.</param>
    public readonly record struct ClipboardXmlResult(
        ClipboardXmlOutcome Outcome,
        IReadOnlyList<XElement> Elements);

    /// <summary>The text form level objects take on the system clipboard.</summary>
    /// <remarks>
    /// Objects go out as bare sibling elements with no wrapper, so a single object is exactly the line
    /// that belongs in a level file. Two or more siblings are not a well-formed XML document, so reading
    /// wraps the text in a synthetic root before parsing.
    /// </remarks>
    public static class ObjectClipboardXml
    {
        /// <summary>Longest clipboard text considered, in characters.</summary>
        /// <remarks>
        /// A level file is a few kilobytes, so 1 MB is far beyond any real selection while keeping a
        /// pathological paste off the UI thread. Over the cap is treated as unrelated content.
        /// </remarks>
        public const int MaxTextLength = 1_048_576;

        private const string WrapperOpen = "<clipboard>";
        private const string WrapperClose = "</clipboard>";

        /// <summary>Serializes objects to the text placed on the system clipboard.</summary>
        /// <param name="elements">The objects to serialize.</param>
        /// <returns>One element per line, in the given order.</returns>
        public static string Write(IEnumerable<XElement> elements)
        {
            return string.Join("\n", elements.Select(e => e.ToString()));
        }

        /// <summary>Reads clipboard text into level objects.</summary>
        /// <param name="text">The system clipboard's text, which may be null.</param>
        /// <param name="descriptors">The table deciding which element names are objects.</param>
        /// <returns>The outcome, plus detached copies of the accepted objects.</returns>
        public static ClipboardXmlResult Read(string? text, DescriptorTable descriptors)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > MaxTextLength)
            {
                return new ClipboardXmlResult(ClipboardXmlOutcome.NotOurs, []);
            }

            // Decided on the raw text, before parsing, because the case this exists to catch is precisely
            // the text that cannot be parsed. A namespaced <html:bubble> fails here too: the prefix stops
            // the name scan, and "html" is not an object.
            if (!NamesAnObject(text, descriptors))
            {
                return new ClipboardXmlResult(ClipboardXmlOutcome.NotOurs, []);
            }

            XDocument document;
            try
            {
                // Wrapped because sibling elements are not a document on their own. DTDs are refused by
                // the reader's default settings, which is what keeps entity-expansion payloads out - do
                // not "fix" a DTD failure by enabling DtdProcessing.
                document = XDocument.Parse(WrapperOpen + StripDeclaration(text) + WrapperClose);
            }
            catch (XmlException)
            {
                return new ClipboardXmlResult(ClipboardXmlOutcome.Rejected, []);
            }

            // Every known descendant at any depth, rather than a whitelist of containers: one rule covers
            // bare siblings, a <layer> chunk and a whole <map>, whose objects sit two levels down. No
            // container name is an object descriptor, so containers are never collected themselves.
            List<XElement> objects =
            [
                .. document.Descendants()
                    .Where(e => e.Name.Namespace == XNamespace.None && descriptors.Knows(e.Name.LocalName))
                    .Select(e => new XElement(e)),
            ];

            return objects.Count > 0
                ? new ClipboardXmlResult(ClipboardXmlOutcome.Parsed, objects)
                : new ClipboardXmlResult(ClipboardXmlOutcome.Rejected, []);
        }

        /// <summary>Removes a leading XML declaration, which cannot appear inside the wrapper.</summary>
        private static string StripDeclaration(string text)
        {
            string trimmed = text.TrimStart();
            if (!trimmed.StartsWith("<?xml", StringComparison.Ordinal))
            {
                return text;
            }

            int end = trimmed.IndexOf("?>", StringComparison.Ordinal);
            return end < 0 ? text : trimmed[(end + 2)..];
        }

        /// <summary>Whether the raw text opens any element the table knows.</summary>
        private static bool NamesAnObject(string text, DescriptorTable descriptors)
        {
            for (int open = text.IndexOf('<'); open >= 0; open = text.IndexOf('<', open + 1))
            {
                int start = open + 1;
                int end = start;
                while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
                {
                    end++;
                }

                if (end > start && descriptors.Knows(text[start..end]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
