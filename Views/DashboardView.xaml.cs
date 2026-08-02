using AutoTable.Controls;
using AutoTable.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AutoTable.Views
{
    public sealed partial class DashboardView : Page
    {
        private readonly DashboardViewModel _vm;

        public DashboardView()
        {
            InitializeComponent();
            _vm = new DashboardViewModel();
            DataContext = _vm;
            Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            // KPI Cards
            KpiPanel.Children.Clear();
            foreach (var metric in _vm.KpiMetrics)
            {
                var card = new KpiCard();
                card.SetMetric(metric.Title, metric.Value, metric.Subtitle, metric.IconGlyph, metric.AccentColor);
                KpiPanel.Children.Add(card);
            }

            // Assessment Progress rows
            var assessments = new[]
            {
                ("Mid Term I — P5 Maths",  "P5", 100, true),
                ("CAT 2 — P4 Science",     "P4",  78, false),
                ("End Term — P6 English",  "P6",  45, false),
                ("CAT 1 — P3 Mathematics", "P3", 100, true),
                ("Mid Term I — P7 SST",    "P7",  92, true),
            };

            bool alt = false;
            foreach (var (name, cls, pct, verified) in assessments)
            {
                var row = BuildAssessmentRow(name, cls, pct, verified, alt);
                AssessmentProgressPanel.Children.Add(row);
                alt = !alt;
            }

            QuickActionsList.ItemsSource = _vm.QuickActions;
            AiInsightsList.ItemsSource = _vm.AiInsights;
            RecentActivityList.ItemsSource = _vm.RecentActivity;
        }

        private static Border BuildAssessmentRow(string name, string cls, int pct, bool verified, bool alt)
        {
            var bg = alt
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252))
                : new SolidColorBrush(Colors.White);

            Windows.UI.Color pctColor = pct >= 90
                ? Windows.UI.Color.FromArgb(255, 22, 163, 74)
                : pct >= 60
                    ? Windows.UI.Color.FromArgb(255, 217, 119, 6)
                    : Windows.UI.Color.FromArgb(255, 220, 38, 38);

            string statusText = verified ? "Verified" : pct >= 100 ? "Complete" : "In Progress";
            Windows.UI.Color statusBg = verified
                ? Windows.UI.Color.FromArgb(255, 220, 252, 231)
                : pct >= 100
                    ? Windows.UI.Color.FromArgb(255, 224, 242, 254)
                    : Windows.UI.Color.FromArgb(255, 254, 243, 199);
            Windows.UI.Color statusFg = verified
                ? Windows.UI.Color.FromArgb(255, 22, 163, 74)
                : pct >= 100
                    ? Windows.UI.Color.FromArgb(255, 2, 132, 199)
                    : Windows.UI.Color.FromArgb(255, 217, 119, 6);

            var grid = new Grid { ColumnSpacing = 12, Padding = new Thickness(16, 10, 16, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var nameBlock = new TextBlock { Text = name, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)) };
            var clsBlock = new TextBlock { Text = cls, FontSize = 13, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)) };
            var pctBlock = new TextBlock { Text = $"{pct}%", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush(pctColor) };
            var bar = new ProgressBar { Value = pct, Maximum = 100, Height = 6, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(pctColor), Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 226, 232, 240)) };
            bar.CornerRadius = new CornerRadius(3);

            var badge = new Border
            {
                Background = new SolidColorBrush(statusBg),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = statusText, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(statusFg) }
            };

            Grid.SetColumn(nameBlock, 0);
            Grid.SetColumn(clsBlock, 1);
            Grid.SetColumn(pctBlock, 2);
            Grid.SetColumn(bar, 3);
            Grid.SetColumn(badge, 4);

            grid.Children.Add(nameBlock);
            grid.Children.Add(clsBlock);
            grid.Children.Add(pctBlock);
            grid.Children.Add(bar);
            grid.Children.Add(badge);

            return new Border { Background = bg, Child = grid };
        }
    }
}
