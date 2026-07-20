using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using AvaloniaDialogs.Views;

using CtrDxEditor.Localization;
using CtrDxEditor.Playtest;

namespace CtrDxEditor.Views
{
    /// <summary>Asks the user to locate Cut the Rope: DX by dragging it in, browsing, or typing a path.</summary>
    /// <remarks>
    /// Drag-and-drop is the primary route everywhere. The secondary route is platform-specific because
    /// of a macOS limitation: a <c>.app</c> is a directory, so <c>OpenFilePickerAsync</c> (which can
    /// only return an <c>IStorageFile</c>) silently discards it, while <c>OpenFolderPickerAsync</c>
    /// refuses to select it because NSOpenPanel treats a bundle as a package. macOS therefore gets a
    /// typed path instead of a Browse button; Windows and Linux get the ordinary file picker.
    /// </remarks>
    public partial class PlaytestLocateDialog : BaseDialog<string?>
    {
        /// <summary>The <see cref="ErrorMessage"/> property.</summary>
        public static readonly StyledProperty<string> ErrorMessageProperty =
            AvaloniaProperty.Register<PlaytestLocateDialog, string>(nameof(ErrorMessage), "");

        private TopLevel? _dropHost;
        private bool _hostAllowedDrop;

        // DragOver fires continuously over the same item, and judging a folder means listing it. The last
        // verdict is kept so that lands on one listing per dragged path rather than one per event.
        private string? _lastJudgedPath;
        private bool _lastJudgedAcceptable;

        /// <summary>Whether this platform uses the typed-path route instead of a file picker.</summary>
        public bool IsMacOS { get; } = OperatingSystem.IsMacOS();

        /// <summary>Explanatory text; the macOS wording covers the app-bundle restriction.</summary>
        public string Body { get; } = Localizer.Get(
            OperatingSystem.IsMacOS() ? "Dialog.Playtest.Locate.BodyMac" : "Dialog.Playtest.Locate.Body");

        /// <summary>Drop-zone prompt, naming the .app on macOS and the program file elsewhere.</summary>
        public string DropHint { get; } = Localizer.Get(
            OperatingSystem.IsMacOS() ? "Dialog.Playtest.Locate.DropHintMac" : "Dialog.Playtest.Locate.DropHint");

        /// <summary>Why the typed or dropped path was rejected, or empty when there is nothing to report.</summary>
        public string ErrorMessage
        {
            get => GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        /// <summary>Creates the locate dialog.</summary>
        public PlaytestLocateDialog()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = this;
        }

        /// <inheritdoc />
        /// <remarks>
        /// The drop is accepted across the whole window rather than on the dialog's drop rectangle.
        /// Dragging from Finder means the source window overlaps the editor, and requiring a release
        /// inside one small target forces the user to rearrange windows first.
        /// </remarks>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (TopLevel.GetTopLevel(this) is { } top)
            {
                _dropHost = top;
                _hostAllowedDrop = DragDrop.GetAllowDrop(top);
                DragDrop.SetAllowDrop(top, true);
                top.AddHandler(DragDrop.DragOverEvent, Host_DragOver);
                top.AddHandler(DragDrop.DropEvent, Host_Drop);
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Window-wide drop is only wanted while this dialog is up, so it is undone on close. The host's
        /// own setting is restored rather than cleared: the editor underneath keeps a window-wide drop of
        /// its own for level XML, and closing this dialog must not switch that off.
        /// </remarks>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (_dropHost is { } top)
            {
                top.RemoveHandler(DragDrop.DragOverEvent, Host_DragOver);
                top.RemoveHandler(DragDrop.DropEvent, Host_Drop);
                DragDrop.SetAllowDrop(top, _hostAllowedDrop);
                _dropHost = null;
            }

            base.OnDetachedFromVisualTree(e);
        }

        // Only a single dropped item is meaningful, and only when it has a local path - a bundle
        // dragged from Finder does, whereas an item from a virtual location may not.
        private static string? LocalPathOf(DragEventArgs e)
        {
            IStorageItem[]? items = e.DataTransfer?.TryGetFiles();
            return items is { Length: 1 } ? items[0].TryGetLocalPath() : null;
        }

        // macOS is held to a bundle because that is what the hint asks for and what every real install
        // looks like there; a dropped plain file is far likelier to be a stray document than a dev build,
        // and dev builds still go in by typing. Windows and Linux ship a plain executable, so the drop
        // there is only asked to resolve.
        private bool IsAcceptableDrop(string path, out string? error)
        {
            if (IsMacOS && !DxExecutableResolver.IsBundleOrBundleContainer(path))
            {
                error = Localizer.Get("Dialog.Playtest.Locate.DropNotBundle");
                return false;
            }

            return DxExecutableResolver.TryResolve(path, out _, out error);
        }

        private bool IsAcceptableDrop(string path)
        {
            if (path != _lastJudgedPath)
            {
                _lastJudgedPath = path;
                _lastJudgedAcceptable = IsAcceptableDrop(path, out _);
            }

            return _lastJudgedAcceptable;
        }

        private void Host_DragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = LocalPathOf(e) is { } path && IsAcceptableDrop(path)
                ? DragDropEffects.Link
                : DragDropEffects.None;
            e.Handled = true;
        }

        // A refused drop reports why rather than doing nothing: the OS cursor says a release will not be
        // taken, but not what was wrong with the item.
        private void Host_Drop(object? sender, DragEventArgs e)
        {
            e.Handled = true;
            if (LocalPathOf(e) is not { } path)
            {
                return;
            }

            if (IsAcceptableDrop(path, out string? error))
            {
                Close(path);
                return;
            }

            ErrorMessage = error ?? Localizer.Get("Dialog.Playtest.Locate.DropNotBundle");
        }

        private void ManualPath_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                AcceptManualPath();
            }
        }

        private void SetManualLocation_Click(object? sender, RoutedEventArgs e)
        {
            AcceptManualPath();
        }

        // Validation runs through the resolver rather than a bare File.Exists, so a typed path has to
        // clear the same bar as a dropped one: the bundle must exist AND contain a runnable binary.
        // The resolver's own message is shown, so the user learns which of those failed.
        private void AcceptManualPath()
        {
            string typed = this.FindControl<TextBox>("ManualPathBox")?.Text?.Trim() ?? "";
            if (typed.Length == 0)
            {
                ErrorMessage = Localizer.Get("Dialog.Playtest.Locate.ManualEmpty");
                return;
            }

            if (DxExecutableResolver.TryResolve(typed, out _, out string? error))
            {
                Close(typed);
                return;
            }

            ErrorMessage = error ?? Localizer.Get("Dialog.Playtest.Locate.ManualEmpty");
        }

        private async void Browse_Click(object? sender, RoutedEventArgs e)
        {
            if (await BrowseAsync() is { } path)
            {
                Close(path);
            }
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        // Windows and Linux only: the executable is an ordinary file there, so it is picked directly.
        private async Task<string?> BrowseAsync()
        {
            if (TopLevel.GetTopLevel(this) is not { } top)
            {
                return null;
            }

            IReadOnlyList<IStorageFile> files = await top.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = Localizer.Get("Dialog.Playtest.Picker.Title"),
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType(Localizer.Get("Dialog.FileType.Executable"))
                        {
                            Patterns = OperatingSystem.IsWindows() ? ["*.exe"] : ["*"],
                        },
                    ],
                });

            return files.Count == 1 ? files[0].TryGetLocalPath() : null;
        }
    }
}
