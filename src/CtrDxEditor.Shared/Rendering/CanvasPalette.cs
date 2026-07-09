using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// The editor-chrome brushes and pens for <see cref="LevelCanvas"/>, resolved from the active theme.
    /// Kept out of the canvas so it holds only interaction/drawing logic; the palette is re-resolved once
    /// per theme change via <see cref="Refresh"/>, never during <see cref="LevelCanvas.Render"/>.
    /// The initial values are the pre-theming literals, used until the first <see cref="Refresh"/> and as
    /// fallbacks if a resource key is ever missing.
    /// </summary>
    internal sealed class CanvasPalette
    {
        /// <summary>Solid backdrop filling the canvas behind the level.</summary>
        public IBrush Background { get; private set; } = new SolidColorBrush(Color.FromRgb(40, 44, 52));

        /// <summary>Outline of the level rectangle.</summary>
        public Pen LevelBorder { get; private set; } = new(new SolidColorBrush(Colors.DimGray), 1);

        /// <summary>Dashed grid lines inside the level rectangle.</summary>
        public Pen Grid { get; private set; } = new(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1)
        {
            DashStyle = new DashStyle([4, 4], 0),
        };

        /// <summary>A grab's auto-catch radius ring.</summary>
        public Pen GrabRadius { get; private set; } = OverlayPen(Brushes.Orange, 1.5);

        /// <summary>A light bulb's lit-radius ring.</summary>
        public Pen BulbRadius { get; private set; } = OverlayPen(Brushes.Gold, 1.5);

        /// <summary>Desktop hitbox overlay.</summary>
        public Pen HitboxDesktop { get; private set; } = OverlayPen(Brushes.LimeGreen, 1.5);

        /// <summary>Phone hitbox overlay.</summary>
        public Pen HitboxPhone { get; private set; } = OverlayPen(Brushes.Magenta, 1.5);

        /// <summary>Selection marquee for the locked object.</summary>
        public Pen ObjectLocked { get; private set; } = OverlayPen(Brushes.Red, 2);

        /// <summary>Selection marquee for an unlocked object.</summary>
        public Pen ObjectSelected { get; private set; } = OverlayPen(Brushes.DeepSkyBlue, 1.5);

        /// <summary>Solid arrow marking a directional force field's push direction (pump, and later steam).
        /// Solid rather than dashed so it reads as an arrow against the dashed hitbox boxes.</summary>
        public Pen ForceArrow { get; private set; } = new(new SolidColorBrush(Color.FromRgb(0x7F, 0x22, 0xFE)), 2);

        /// <summary>Solid curved arrow marking a rotateSpeed-backed object spin.</summary>
        public Pen SpinArrow { get; private set; } = new(new SolidColorBrush(Color.FromRgb(0x0E, 0x74, 0x9A)), 2);

        /// <summary>Dotted circular path marking RC/RW orbital movement.</summary>
        public Pen OrbitPath { get; private set; } = DottedPen(Color.FromRgb(0x25, 0x63, 0xEB), 1.5);

        /// <summary>Solid direction arrow for RC/RW orbital movement.</summary>
        public Pen OrbitPathArrow { get; private set; } = SolidPen(Color.FromRgb(0x25, 0x63, 0xEB), 2.25);

        /// <summary>Text brush for a timed-star duration label on the blank (no-background) canvas:
        /// pure white in the dark theme, pure black in the light theme. When a background is applied
        /// the canvas draws these labels black regardless.</summary>
        public IBrush StarDurationText { get; private set; } = Brushes.White;

        /// <summary>Re-resolves every brush and pen for <paramref name="host"/>'s active theme variant.</summary>
        public void Refresh(Control host)
        {
            Background = new SolidColorBrush(ThemeColor(host, "EditorColor.SurfacePanel", Color.FromRgb(40, 44, 52)));
            LevelBorder = new Pen(new SolidColorBrush(ThemeColor(host, "EditorColor.CanvasBorder", Colors.DimGray)), 1);
            Grid = new Pen(new SolidColorBrush(ThemeColor(host, "EditorColor.CanvasGrid", Color.FromArgb(40, 255, 255, 255))), 1)
            {
                DashStyle = new DashStyle([4, 4], 0),
            };

            GrabRadius = OverlayPen(ThemeColor(host, "EditorColor.OverlayGrabRadius", Colors.Orange), 1.5);
            BulbRadius = OverlayPen(ThemeColor(host, "EditorColor.OverlayBulbRadius", Colors.Gold), 1.5);
            HitboxDesktop = OverlayPen(ThemeColor(host, "EditorColor.OverlayHitboxDesktop", Colors.LimeGreen), 1.5);
            HitboxPhone = OverlayPen(ThemeColor(host, "EditorColor.OverlayHitboxPhone", Colors.Magenta), 1.5);
            ObjectLocked = OverlayPen(ThemeColor(host, "EditorColor.OverlayObjectLocked", Colors.Red), 2);
            ObjectSelected = OverlayPen(ThemeColor(host, "EditorColor.OverlayObjectSelected", Colors.DeepSkyBlue), 1.5);
            ForceArrow = new Pen(new SolidColorBrush(ThemeColor(host, "EditorColor.OverlayForceArrow", Color.FromRgb(0x7F, 0x22, 0xFE))), 2);
            SpinArrow = new Pen(new SolidColorBrush(ThemeColor(host, "EditorColor.OverlaySpinArrow", Color.FromRgb(0x0E, 0x74, 0x9A))), 2);
            Color orbitColor = ThemeColor(host, "EditorColor.OverlayOrbitPath", Color.FromRgb(0x25, 0x63, 0xEB));
            OrbitPath = DottedPen(orbitColor, 1.5);
            OrbitPathArrow = SolidPen(orbitColor, 2.25);
            StarDurationText = host.ActualThemeVariant == ThemeVariant.Dark ? Brushes.White : Brushes.Black;
        }

        /// <summary>Resolves a themed <see cref="Color"/> resource for the host's active theme variant,
        /// falling back to <paramref name="fallback"/> when the key is missing.</summary>
        private static Color ThemeColor(Control host, string key, Color fallback)
        {
            return host.TryFindResource(key, host.ActualThemeVariant, out object? value) && value is Color color
                ? color
                : fallback;
        }

        /// <summary>Builds a dashed overlay pen with the shared editor dash pattern.</summary>
        private static Pen OverlayPen(IBrush brush, double thickness)
        {
            return new Pen(brush, thickness) { DashStyle = new DashStyle([4, 3], 0) };
        }

        /// <summary>Builds a dashed overlay pen from a solid color.</summary>
        private static Pen OverlayPen(Color color, double thickness)
        {
            return OverlayPen(new SolidColorBrush(color), thickness);
        }

        /// <summary>Builds a dotted overlay pen from a solid color.</summary>
        private static Pen DottedPen(Color color, double thickness)
        {
            return new Pen(new SolidColorBrush(color), thickness) { DashStyle = new DashStyle([1, 3], 0) };
        }

        /// <summary>Builds a solid overlay pen from a solid color.</summary>
        private static Pen SolidPen(Color color, double thickness)
        {
            return new Pen(new SolidColorBrush(color), thickness);
        }
    }
}
