using AutoTable.Controls;
using AutoTable.Converters;
using AutoTable.ViewModels;
using AutoTable.Services;
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

        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            // KPI Cards
            KpiPanel.Children.Clear();
            foreach (var metric in _vm.KpiMetrics)
            {
                var card = new KpiCard();
                card.SetMetric(metric.Title, metric.Value, metric.Subtitle, metric.IconGlyph, metric.AccentColor);
                KpiPanel.Children.Add(card);
            }

            QuickActionsList.ItemsSource = _vm.QuickActions;
            AiInsightsList.ItemsSource = _vm.AiInsights;
            RecentActivityList.ItemsSource = _vm.RecentActivity;

            // Load real data for Assessment Progress and Performance Trend
            await LoadAssessmentProgressAsync();
            await LoadPerformanceTrendAsync();
        }

        private async System.Threading.Tasks.Task LoadAssessmentProgressAsync()
        {
            AssessmentProgressPanel.Children.Clear();
            try
            {                var ds = AppServices.DataService;
                if (ds == null) return;

                var assessments = await ds.GetAssessmentsAsync();
                if (assessments.Count == 0)
                {
                    AssessmentProgressPanel.Children.Add(new TextBlock
                    {
                        Text = "No assessments created yet.",
                        FontSize = 13,
                        Foreground = ThemeResourceHelper.GetThemeBrush("TextMutedBrush"),
                        Padding = new Thickness(16, 12, 16, 12)
                    });
                    return;
                }

                // Show the most recent assessments first, up to 8
                var sorted = assessments.OrderByDescending(a => a.DueDate).Take(8).ToList();
                bool alt = false;
                foreach (var a in sorted)                {
                    var displayName = string.IsNullOrEmpty(a.Subject)
                        ? a.Name
                        : $"{a.Name} — {a.Subject}";
                    var row = BuildAssessmentRow(displayName, a.ClassName, a.MarksEnteredPercent, a.IsVerified, alt);
                    AssessmentProgressPanel.Children.Add(row);
                    alt = !alt;
                }
            }
            catch
            {
                AssessmentProgressPanel.Children.Add(new TextBlock
                {
                    Text = "Failed to load assessments.",
                    FontSize = 13,
                    Foreground = ThemeResourceHelper.GetThemeBrush("TextMutedBrush"),
                    Padding = new Thickness(16, 12, 16, 12)
                });
            }
        }

        private async System.Threading.Tasks.Task LoadPerformanceTrendAsync()
        {
            // Replace the static placeholder bars with real gradebook averages.
            // Compute per-class averages across all subjects and render as a bar chart.
            try
            {
                var ds = AppServices.DataService;
                if (ds == null) return;

                var classes = await ds.GetClassesAsync();
                var subjects = await ds.GetSubjectsAsync();
                if (classes.Count == 0) return;

                var classAverages = new List<(string Name, double Avg)>();
                foreach (var cls in classes)
                {
                    double totalAvg = 0;
                    int count = 0;
                    foreach (var subj in subjects)
                    {
                        var rows = await ds.GetGradebookAsync(cls.Name, subj.Name);
                        if (rows.Count > 0)
                        {
                            totalAvg += rows.Average(r => r.Average);
                            count++;
                        }
                    }
                    if (count > 0)
                        classAverages.Add((cls.Name, totalAvg / count));
                }

                if (classAverages.Count == 0) return;

                // Find the placeholder panel inside the Performance Trend card and replace its content
                var trendPanel = FindName("TrendChartPlaceholder") as Microsoft.UI.Xaml.Controls.StackPanel;
                if (trendPanel == null) return;

                trendPanel.Children.Clear();
                foreach (var (name, avg) in classAverages.OrderByDescending(x => x.Avg))
                {
                    var grid = new Grid { Height = 22 };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

                    grid.Children.Add(new TextBlock
                    {
                        Text = name,
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = ThemeResourceHelper.GetThemeBrush("TextSecondaryBrush"),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });

                    var pct = Math.Min(avg, 100);
                    var barFill = new Border
                    {
                        Height = 10,
                        CornerRadius = new CornerRadius(5),
                        Background = ThemeResourceHelper.GetThemeBrush("PrimaryBlueBrush"),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Width = pct / 100.0 * 200
                    };
                    var barBg = new Border
                    {
                        Height = 10,
                        CornerRadius = new CornerRadius(5),
                        Background = ThemeResourceHelper.GetThemeBrush("SurfaceGray2Brush"),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(6, 0, 6, 0),
                        Child = barFill
                    };

                    grid.Children.Add(barBg);
                    grid.Children.Add(new TextBlock
                    {
                        Text = $"{avg:F1}%",
                        FontSize = 11,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Foreground = ThemeResourceHelper.GetThemeBrush("TextPrimaryBrush")
                    });

                    Grid.SetColumn((Microsoft.UI.Xaml.FrameworkElement)grid.Children[0], 0);
                    Grid.SetColumn((Microsoft.UI.Xaml.FrameworkElement)grid.Children[1], 1);
                    Grid.SetColumn((Microsoft.UI.Xaml.FrameworkElement)grid.Children[2], 2);
                    trendPanel.Children.Add(grid);
                }
            }
            catch
            {
                // Gracefully degrade — keep the static placeholder if data load fails
            }
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

        // Simple search result model used by the AutoSuggestBox
        private class SearchResult
        {
            public string Title { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // e.g., "Student", "Class", "Teacher"
            public object? Payload { get; set; }
        }

        private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            var q = sender.Text?.Trim() ?? string.Empty;
            if (q.Length < 2)
            {
                sender.ItemsSource = null;
                return;
            }

            try
            {
                var ds = AppServices.DataService;
                var results = new List<SearchResult>();
                if (ds != null)
                {
                    var students = await ds.GetAllStudentsAsync();
                    foreach (var s in students.Where(s => s.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).Take(8))
                        results.Add(new SearchResult { Title = s, Type = "Student", Payload = s });

                    var classes = await ds.GetClassesAsync();
                    foreach (var c in classes.Where(c => c.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).Take(8))
                        results.Add(new SearchResult { Title = c.Name, Type = "Class", Payload = c });

                    var teachers = await ds.GetTeachersAsync();
                    foreach (var t in teachers.Where(t => t.FullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).Take(8))
                        results.Add(new SearchResult { Title = t.FullName, Type = "Teacher", Payload = t });
                }

                sender.ItemsSource = results;
            }
            catch
            {
                sender.ItemsSource = null;
            }
        }

        private async void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is not SearchResult r) return;

            try
            {
                var dlg = new ContentDialog
                {
                    Title = r.Title,
                    Content = r.Type,
                    PrimaryButtonText = "Open",
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };

                var res = await dlg.ShowAsync();
                if (res == ContentDialogResult.Primary)
                {
                    var tag = r.Type switch
                    {
                        "Student" => "Students",
                        "Class"   => "Classes",
                        "Teacher" => "Teachers",
                        _         => string.Empty
                    };
                    if (!string.IsNullOrEmpty(tag))
                        NavigationService.Instance.NavigateToShellPage(tag);
                }
            }
            catch { }
        }

        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Content is not string action) return;

            // Map quick actions to shell route tags. Navigation goes through the
            // shell's CONTENT frame (NavigateToShellPage) so the sidebar persists.
            // Never use the root-frame Navigate() here — it replaces the whole
            // ShellView and the sidebar disappears.
            var tag = action switch
            {
                "Enter Marks"          => "MarksEntry",
                "Add Assessment"       => "Assessments",
                "View Gradebook"       => "Gradebook",
                "Generate Report Card" => "ReportCards",
                "Create Term"          => "TermManagement",
                "Record Fees Payment"  => "FeeCollection",
                _                      => string.Empty
            };

            if (!string.IsNullOrEmpty(tag))
                NavigationService.Instance.NavigateToShellPage(tag);
        }
    }
}