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
        /// <summary>The <see cref="ManualError"/> property.</summary>
        public static readonly StyledProperty<string> ManualErrorProperty =
            AvaloniaProperty.Register<PlaytestLocateDialog, string>(nameof(ManualError), "");

        private TopLevel? _dropHost;

        /// <summary>Whether this platform uses the typed-path route instead of a file picker.</summary>
        public bool IsMacOS { get; } = OperatingSystem.IsMacOS();

        /// <summary>Explanatory text; the macOS wording covers the app-bundle restriction.</summary>
        public string Body { get; } = Localizer.Get(
            OperatingSystem.IsMacOS() ? "Dialog.Playtest.Locate.BodyMac" : "Dialog.Playtest.Locate.Body");

        /// <summary>Drop-zone prompt, naming the .app on macOS and the program file elsewhere.</summary>
        public string DropHint { get; } = Localizer.Get(
            OperatingSystem.IsMacOS() ? "Dialog.Playtest.Locate.DropHintMac" : "Dialog.Playtest.Locate.DropHint");

        /// <summary>Why the typed path was rejected, or empty when there is nothing to report.</summary>
        public string ManualError
        {
            get => GetValue(ManualErrorProperty);
            set => SetValue(ManualErrorProperty, value);
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
                DragDrop.SetAllowDrop(top, true);
                top.AddHandler(DragDrop.DragOverEvent, Host_DragOver);
                top.AddHandler(DragDrop.DropEvent, Host_Drop);
            }
        }

        /// <inheritdoc />
        /// <remarks>Window-wide drop is only wanted while this dialog is up, so it is undone on close.</remarks>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (_dropHost is { } top)
            {
                top.RemoveHandler(DragDrop.DragOverEvent, Host_DragOver);
                top.RemoveHandler(DragDrop.DropEvent, Host_Drop);
                DragDrop.SetAllowDrop(top, false);
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

        private void Host_DragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = LocalPathOf(e) is null ? DragDropEffects.None : DragDropEffects.Link;
            e.Handled = true;
        }

        private void Host_Drop(object? sender, DragEventArgs e)
        {
            e.Handled = true;
            if (LocalPathOf(e) is { } path)
            {
                Close(path);
            }
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
                ManualError = Localizer.Get("Dialog.Playtest.Locate.ManualEmpty");
                return;
            }

            if (DxExecutableResolver.TryResolve(typed, out _, out string? error))
            {
                Close(typed);
                return;
            }

            ManualError = error ?? Localizer.Get("Dialog.Playtest.Locate.ManualEmpty");
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
