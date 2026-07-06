using System;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace CtrDxEditor.Controls
{
    /// <summary>
    /// A <see cref="TextBox"/> that accepts whole numbers only: invalid characters cannot be typed,
    /// pasted, or dropped, and the value is bounded to <see cref="Minimum"/>..<see cref="Maximum"/>.
    /// No spinner - it is a plain box that simply refuses non-numeric input.
    /// </summary>
    public class NumericTextBox : TextBox
    {
        /// <summary>Backs <see cref="Minimum"/>.</summary>
        public static readonly StyledProperty<int> MinimumProperty =
            AvaloniaProperty.Register<NumericTextBox, int>(nameof(Minimum), -9999);

        /// <summary>Backs <see cref="Maximum"/>.</summary>
        public static readonly StyledProperty<int> MaximumProperty =
            AvaloniaProperty.Register<NumericTextBox, int>(nameof(Maximum), 9999);

        // Adopt the stock TextBox theme/styles rather than looking for a NumericTextBox one.
        protected override Type StyleKeyOverride => typeof(TextBox);

        /// <summary>Smallest value the box will accept.</summary>
        public int Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

        /// <summary>Largest value the box will accept.</summary>
        public int Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

        /// <summary>
        /// Whether <paramref name="text"/> is an acceptable value or a prefix of one: empty, a lone
        /// "-" (when negatives are allowed), or an integer within [<paramref name="min"/>,
        /// <paramref name="max"/>]. Intermediate states are allowed so a value can be typed digit by
        /// digit; anything containing a non-digit, or out of range, is rejected.
        /// </summary>
        public static bool IsAcceptable(string text, int min, int max)
        {
            if (text.Length == 0)
            {
                return true;
            }

            int i = 0;
            if (text[0] == '-')
            {
                if (min >= 0)
                {
                    return false;
                }
                if (text.Length == 1)
                {
                    return true; // a lone minus is a valid prefix
                }
                i = 1;
            }

            for (; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                {
                    return false;
                }
            }

            return long.TryParse(text, out long value) && value >= min && value <= max;
        }

        /// <inheritdoc />
        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (e.Text is { Length: > 0 } typed && !WouldBeAcceptable(typed))
            {
                e.Handled = true;
                return;
            }
            base.OnTextInput(e);
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Paste bypasses OnTextInput, so intercept the platform paste gesture and filter it.
            if (Application.Current?.PlatformSettings?.HotkeyConfiguration.Paste is { } paste
                && paste.Any(g => g.Matches(e)))
            {
                e.Handled = true;
                _ = PasteFilteredAsync();
                return;
            }
            base.OnKeyDown(e);
        }

        private async Task PasteFilteredAsync()
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
            {
                return;
            }

            string? text = await clipboard.TryGetTextAsync();
            if (!string.IsNullOrEmpty(text) && WouldBeAcceptable(text))
            {
                ReplaceSelection(text);
            }
        }

        // Whether replacing the current selection with insert yields an acceptable string.
        private bool WouldBeAcceptable(string insert)
        {
            return IsAcceptable(Prospective(insert), Minimum, Maximum);
        }

        private string Prospective(string insert)
        {
            string current = Text ?? string.Empty;
            int start = Math.Clamp(Math.Min(SelectionStart, SelectionEnd), 0, current.Length);
            int end = Math.Clamp(Math.Max(SelectionStart, SelectionEnd), 0, current.Length);
            return current[..start] + insert + current[end..];
        }

        private void ReplaceSelection(string insert)
        {
            string current = Text ?? string.Empty;
            int start = Math.Clamp(Math.Min(SelectionStart, SelectionEnd), 0, current.Length);
            int end = Math.Clamp(Math.Max(SelectionStart, SelectionEnd), 0, current.Length);
            Text = current[..start] + insert + current[end..];
            CaretIndex = start + insert.Length;
        }
    }
}
