using System;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;

namespace CtrDxEditor.Controls
{
    /// <summary>
    /// Displays clipped, single-line text that bounces horizontally while active when the text overflows.
    /// </summary>
    public sealed class MarqueeTextBlock : Control, IDisposable
    {
        private static readonly TimeSpan MinLeg = TimeSpan.FromSeconds(0.6);

        /// <summary>Identifies the <see cref="Text"/> styled property.</summary>
        public static readonly StyledProperty<string?> TextProperty =
            AvaloniaProperty.Register<MarqueeTextBlock, string?>(nameof(Text));

        /// <summary>Identifies the <see cref="ForceActive"/> styled property.</summary>
        public static readonly StyledProperty<bool> ForceActiveProperty =
            AvaloniaProperty.Register<MarqueeTextBlock, bool>(nameof(ForceActive));

        private readonly TextBlock _label;
        private readonly TranslateTransform _translate = new();
        private readonly DispatcherTimer _animationTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
        private readonly Stopwatch _animationClock = new();
        private double _overflow;

        /// <summary>Initializes a new instance of the <see cref="MarqueeTextBlock"/> class.</summary>
        public MarqueeTextBlock()
        {
            ClipToBounds = true;
            _label = new TextBlock
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                RenderTransform = _translate,
            };
            _label[!TextBlock.TextProperty] = this[!TextProperty];
            _label[!TextBlock.ForegroundProperty] = this[!TextElement.ForegroundProperty];
            VisualChildren.Add(_label);
            LogicalChildren.Add(_label);
            _animationTimer.Tick += OnAnimationTick;
        }

        /// <summary>Gets or sets the text displayed by the control.</summary>
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>Gets or sets whether the marquee remains active when the pointer is not over it.</summary>
        public bool ForceActive
        {
            get => GetValue(ForceActiveProperty);
            set => SetValue(ForceActiveProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == IsPointerOverProperty
                || change.Property == ForceActiveProperty)
            {
                UpdateAnimation();
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            _label.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            return new Size(0, _label.DesiredSize.Height);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            double textWidth = _label.DesiredSize.Width;
            _label.Arrange(new Rect(0, 0, textWidth, finalSize.Height));
            _overflow = MarqueeMath.Overflow(textWidth, finalSize.Width);
            UpdateAnimation();
            return finalSize;
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            StopAnimation();
            base.OnDetachedFromVisualTree(e);
        }

        /// <summary>Releases animation cancellation resources owned by this control.</summary>
        public void Dispose()
        {
            StopAnimation();
            _animationTimer.Tick -= OnAnimationTick;
        }

        private bool IsActive => IsPointerOver || ForceActive;

        private void UpdateAnimation()
        {
            StopAnimation();
            if (!IsActive || _overflow <= 0)
            {
                _translate.X = 0;
                return;
            }

            _animationClock.Restart();
            _animationTimer.Start();
        }

        private void StopAnimation()
        {
            _animationTimer.Stop();
            _animationClock.Reset();
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            _translate.X = MarqueeMath.BounceOffset(
                _overflow,
                _animationClock.Elapsed.TotalSeconds,
                MarqueeMath.DefaultSpeed,
                MinLeg.TotalSeconds,
                MarqueeMath.DefaultPauseSeconds);
        }
    }
}
