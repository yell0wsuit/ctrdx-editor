using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using CtrDxEditor.Views;

using DialogHostAvalonia;

namespace CtrDxEditor.Controls
{
    /// <summary>A standard contextual-help button that opens a touch-friendly modal dialog.</summary>
    public partial class HelpButton : UserControl
    {
        /// <summary>The dialog heading property.</summary>
        public static readonly StyledProperty<string> HeaderProperty =
            AvaloniaProperty.Register<HelpButton, string>(nameof(Header), string.Empty);

        /// <summary>The dialog body property.</summary>
        public static readonly StyledProperty<string> MessageProperty =
            AvaloniaProperty.Register<HelpButton, string>(nameof(Message), string.Empty);

        /// <summary>Creates the button and loads its XAML.</summary>
        public HelpButton()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>The heading shown in the help dialog.</summary>
        public string Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>The explanatory text shown in the help dialog.</summary>
        public string Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        private async void Help_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Message))
            {
                return;
            }

            MessageDialog dialog = new()
            {
                Header = Header,
                Message = Message,
            };

            DialogSession? parentSession = DialogHost.GetDialogSession(null);
            DialogBackdrop? backdrop = DialogBackdrop.InsertAfter(parentSession?.Host);

            try
            {
                _ = await DialogHost.Show(dialog);
            }
            finally
            {
                backdrop?.Detach();
            }
        }
    }
}
