using System.Runtime.InteropServices.JavaScript;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Browser.Content
{
    /// <summary>
    /// Answers the page's <c>beforeunload</c> handler, so closing the tab with unsaved level changes
    /// raises the browser's confirmation instead of discarding them silently.
    /// </summary>
    /// <remarks>
    /// The desktop head covers this in <c>MainWindow.OnClosing</c>, and New, Open and Close all
    /// prompt through <c>UnsavedChangesPrompt</c>. The browser head has no equivalent: it runs on the
    /// single-view lifetime, which has no closing event of its own, so the page has to ask.
    /// <para>
    /// Unlike the rest of this folder the call runs the other way, from JavaScript into managed code,
    /// because <c>beforeunload</c> has to decide synchronously and <see cref="EditorViewModel.IsModified"/>
    /// is computed on demand rather than raising change notifications to push at the page.
    /// </para>
    /// </remarks>
    internal static partial class UnsavedChangesInterop
    {
        /// <summary>
        /// Whether the open level has edits that have not been saved. False before the editor has
        /// mounted, and whenever no level is open.
        /// </summary>
        [JSExport]
        internal static bool HasUnsavedChanges()
        {
            return Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime
            {
                MainView.DataContext: EditorViewModel { IsModified: true },
            };
        }
    }
}
