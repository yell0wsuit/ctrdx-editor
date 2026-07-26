using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

using CtrDxEditor.UsageGuide;

namespace CtrDxEditor.Controls
{
    /// <summary>A text block that renders the Usage Guide's constrained inline emphasis syntax.</summary>
    public sealed class GuideTextBlock : TextBlock
    {
        private static readonly FontFamily InterFontFamily =
            new("avares://CtrDxEditor.Shared/Assets/Fonts/Inter/#Inter");

        /// <summary>Defines the localized body copy containing optional emphasis markers.</summary>
        public static readonly StyledProperty<string> MarkupProperty =
            AvaloniaProperty.Register<GuideTextBlock, string>(nameof(Markup), string.Empty);

        /// <summary>Creates a guide text block backed by the bundled Inter family.</summary>
        public GuideTextBlock()
        {
            FontFamily = InterFontFamily;
        }

        /// <summary>Localized body copy containing optional emphasis markers.</summary>
        public string Markup
        {
            get => GetValue(MarkupProperty);
            set => SetValue(MarkupProperty, value);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == MarkupProperty)
            {
                RenderMarkup(change.GetNewValue<string>());
            }
        }

        private void RenderMarkup(string text)
        {
            InlineCollection inlines = [];
            foreach (GuideTextRun segment in GuideTextParser.Parse(text))
            {
                Run run = new(segment.Text);
                if (segment.IsBold)
                {
                    run.FontWeight = FontWeight.Bold;
                }

                if (segment.IsItalic)
                {
                    run.FontFamily = InterFontFamily;
                    run.FontStyle = FontStyle.Italic;
                }

                inlines.Add(run);
            }

            Inlines = inlines;
        }
    }
}
