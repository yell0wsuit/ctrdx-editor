using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CtrDxEditor.Core.Document
{
    /// <summary>The text form level objects take on the system clipboard.</summary>
    /// <remarks>
    /// Outbound only: objects are published so they can be pasted into a level file, a chat or another
    /// tool, and nothing is ever read back in. Paste inside the editor works from the in-app buffer, which
    /// is why it needs no guess about what the system clipboard currently holds.
    /// <para>
    /// Objects go out as bare sibling elements with no wrapper, so a single object is exactly the line
    /// that belongs in a level file.
    /// </para>
    /// </remarks>
    public static class ObjectClipboardXml
    {
        /// <summary>Serializes objects to the text placed on the system clipboard.</summary>
        /// <param name="elements">The objects to serialize.</param>
        /// <returns>One element per line, in the given order.</returns>
        public static string Write(IEnumerable<XElement> elements)
        {
            return string.Join("\n", elements.Select(e => e.ToString()));
        }
    }
}
