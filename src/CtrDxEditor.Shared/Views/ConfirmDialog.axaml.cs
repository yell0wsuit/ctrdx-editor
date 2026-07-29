using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using AvaloniaDialogs.Views;

namespace CtrDxEditor.Views
{
    /// <summary>A headed confirmation dialog with explicit confirm and cancel actions.</summary>
    public partial class ConfirmDialog : BaseDialog<bool>
    {
        /// <summary>The <see cref="Header"/> property.</summary>
        public static readonly StyledProperty<string> HeaderProperty =
            AvaloniaProperty.Register<ConfirmDialog, string>(nameof(Header));

        /// <summary>The <see cref="Message"/> property.</summary>
        public static readonly StyledProperty<string> MessageProperty =
            AvaloniaProperty.Register<ConfirmDialog, string>(nameof(Message));

        /// <summary>The <see cref="PositiveText"/> property.</summary>
        public static readonly StyledProperty<string> PositiveTextProperty =
            AvaloniaProperty.Register<ConfirmDialog, string>(nameof(PositiveText));

        /// <summary>The <see cref="NegativeText"/> property.</summary>
        public static readonly StyledProperty<string> NegativeTextProperty =
            AvaloniaProperty.Register<ConfirmDialog, string>(nameof(NegativeText));

        /// <summary>The <see cref="IsDestructive"/> property.</summary>
        public static readonly StyledProperty<bool> IsDestructiveProperty =
            AvaloniaProperty.Register<ConfirmDialog, bool>(nameof(IsDestructive), defaultValue: true);

        /// <summary>The dialog's title line.</summary>
        public string Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>The dialog's body text.</summary>
        public string Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        /// <summary>The label for the confirming action.</summary>
        public string PositiveText
        {
            get => GetValue(PositiveTextProperty);
            set => SetValue(PositiveTextProperty, value);
        }

        /// <summary>The label for the canceling action.</summary>
        public string NegativeText
        {
            get => GetValue(NegativeTextProperty);
            set => SetValue(NegativeTextProperty, value);
        }

        /// <summary>
        /// Whether confirming discards work or is otherwise hard to undo, which styles the confirming
        /// button as a warning. Defaults to <see langword="true"/>, since most confirmations are asked
        /// precisely because the answer is destructive.
        /// </summary>
        public bool IsDestructive
        {
            get => GetValue(IsDestructiveProperty);
            set => SetValue(IsDestructiveProperty, value);
        }

        /// <summary>Creates the confirmation dialog.</summary>
        public ConfirmDialog()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = this;
        }

        private void Confirm_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
