using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AutoTable.Controls
{
    public sealed partial class KpiCard : UserControl
    {
        public KpiCard()
        {
            InitializeComponent();
        }

        public void SetMetric(string title, string value, string subtitle, string iconGlyph, string accentColor)
        {
            TitleBlock.Text = title;
            ValueBlock.Text = value;
            SubtitleBlock.Text = subtitle;
            IconBlock.Glyph = iconGlyph;

            var color = ParseColor(accentColor);
            var brush = new SolidColorBrush(color);
            AccentBar.Background = brush;
            IconBlock.Foreground = brush;
        }

        private static Windows.UI.Color ParseColor(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                return Windows.UI.Color.FromArgb(
                    255,
                    System.Convert.ToByte(hex.Substring(0, 2), 16),
                    System.Convert.ToByte(hex.Substring(2, 2), 16),
                    System.Convert.ToByte(hex.Substring(4, 2), 16));
            }
            return Windows.UI.Color.FromArgb(255, 29, 111, 232);
        }
    }
}
