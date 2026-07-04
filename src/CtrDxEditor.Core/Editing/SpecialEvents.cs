using System;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Seasonal event windows, ported from Cut the Rope: DX's <c>SpecialEvents</c>.</summary>
    public static class SpecialEvents
    {
        /// <summary>Christmas event period: December and January (matches the game).</summary>
        public static bool IsXmas => DateTime.Now.Month is 12 or 1;
    }
}
