using AutoTable.Controls;
using AutoTable.Converters;
using AutoTable.ViewModels;
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
                ? ThemeResourceHelper.GetThemeBrush("TableRowAltBrush")
                : ThemeResourceHelper.GetThemeBrush("SurfaceWhiteBrush");

            var pctBrush = pct >= 90
                ? ThemeResourceHelper.GetThemeBrush("SuccessGreenBrush")
                : pct >= 60
                    ? ThemeResourceHelper.GetThemeBrush("WarningOrangeBrush")
                    : ThemeResourceHelper.GetThemeBrush("DangerRedBrush");

            string statusText = verified ? "Verified" : pct >= 100 ? "Complete" : "In Progress";
            var statusBg = verified
                ? ThemeResourceHelper.GetThemeBrush("GreenSubtleBrush")
                : pct >= 100
                    ? ThemeResourceHelper.GetThemeBrush("BlueSubtleBrush")
                    : ThemeResourceHelper.GetThemeBrush("OrangeSubtleBrush");
            var statusFg = verified
                ? ThemeResourceHelper.GetThemeBrush("SuccessGreenBrush")
                : pct >= 100
                    ? ThemeResourceHelper.GetThemeBrush("PrimaryBlueBrush")
                    : ThemeResourceHelper.GetThemeBrush("WarningOrangeBrush");

            var grid = new Grid { ColumnSpacing = 12, Padding = new Thickness(16, 10, 16, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var nameBlock = new TextBlock { Text = name, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Foreground = ThemeResourceHelper.GetThemeBrush("TextPrimaryBrush") };
            var clsBlock = new TextBlock { Text = cls, FontSize = 13, VerticalAlignment = VerticalAlignment.Center, Foreground = ThemeResourceHelper.GetThemeBrush("TextSecondaryBrush") };
            var pctBlock = new TextBlock { Text = $"{pct}%", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = pctBrush };
            var bar = new ProgressBar { Value = pct, Maximum = 100, Height = 6, VerticalAlignment = VerticalAlignment.Center, Foreground = pctBrush, Background = ThemeResourceHelper.GetThemeBrush("SurfaceGray2Brush") };
            bar.CornerRadius = new CornerRadius(3);

            var badge = new Border
            {
                Background = statusBg,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = statusText, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = statusFg }
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

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            _vm.SearchText = sender.Text;
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Handle suggestion chosen - navigate or filter
            var selectedItem = args.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedItem))
            {
                // Could navigate to relevant page or show details
            }
        }
    }
}