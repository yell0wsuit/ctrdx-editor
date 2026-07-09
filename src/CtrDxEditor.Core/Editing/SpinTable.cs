using System.Collections.Generic;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Registry of objects whose <c>rotateSpeed</c> attribute is exposed as editor spin.</summary>
    public static class SpinTable
    {
        private static readonly HashSet<string> Elements =
        [
            "star",
            "spike1",
            "spike2",
            "spike3",
            "spike4",
            "electro",
        ];

        /// <summary>Whether <paramref name="element"/> can expose spin controls.</summary>
        /// <param name="element">XML element name to look up.</param>
        /// <returns><see langword="true"/> when the editor should show spin fields.</returns>
        public static bool IsSpinnable(string element)
        {
            return Elements.Contains(element);
        }
    }
}
