using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using AvaloniaDialogs.Views;

namespace CtrDxEditor.Views
{
    /// <summary>A titled message dialog with a single OK button, shown nested on top of another dialog.</summary>
    public partial class MessageDialog : BaseDialog
    {
        /// <summary>The <see cref="Header"/> property.</summary>
        public static readonly StyledProperty<string> HeaderProperty =
            AvaloniaProperty.Register<MessageDialog, string>(nameof(Header));

        /// <summary>The <see cref="Message"/> property.</summary>
        public static readonly StyledProperty<string> MessageProperty =
            AvaloniaProperty.Register<MessageDialog, string>(nameof(Message));

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

        /// <summary>Creates the message dialog.</summary>
        public MessageDialog()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = this;
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
