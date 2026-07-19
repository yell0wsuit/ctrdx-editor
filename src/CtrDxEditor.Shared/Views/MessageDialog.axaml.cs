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

        /// <summary>The <see cref="IsDanger"/> property.</summary>
        public static readonly StyledProperty<bool> IsDangerProperty =
            AvaloniaProperty.Register<MessageDialog, bool>(nameof(IsDanger));

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

        /// <summary>Whether the header uses the danger color reserved for actual errors.</summary>
        public bool IsDanger
        {
            get => GetValue(IsDangerProperty);
            set => SetValue(IsDangerProperty, value);
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
