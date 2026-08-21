using AutoTable.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoTable.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace AutoTable.Views
{
    public sealed partial class ShellView : Page
    {
        private readonly ShellViewModel _vm;
        private Button? _activeNavButton;

        private static readonly Dictionary<string, (string Title, string Subtitle)> PageMeta = new()
        {
            ["Dashboard"]          = ("Marks Management Dashboard", "Overview of assessments, marks entry, and student performance."),
            ["Assessments"]        = ("Assessments", "Create, track, and manage class assessments and weightings."),
            ["MarksEntry"]         = ("Marks Entry", "Enter and update student marks for selected assessments."),
            ["Gradebook"]          = ("Gradebook", "View consolidated marks, averages, ranks, and grades by class."),
            ["StudentPerformance"] = ("Student Performance", "Individual student performance summaries and trends."),
            ["Analytics"]          = ("Analytics", "Hierarchical performance analytics across classes and time."),
            ["Moderation"]         = ("Moderation", "Review and approve marks before publishing."),
            ["ReportCards"]        = ("Report Cards", "Generate and print student report cards."),
            ["Students"]           = ("Students", "Manage student records, LIN identifiers, and termination."),
            ["Classes"]            = ("Classes & Subjects", "Manage classes, subjects, and subject assignments."),
            ["AuditLog"]           = ("Termination / Audit Log", "View termination history and anonymization records."),
            ["FinDashboard"]       = ("Financial Dashboard", "Overview of fee collection, budget, and expenditure."),
            ["FeeCollection"]      = ("Fee Collection", "Track and manage student fee payments."),
            ["Budget"]             = ("Budget & Expenditure", "School budget planning and expenditure tracking."),
            ["AiInsights"]         = ("AI Insights", "AI-assisted recommendations and workflow automations."),
        };

        private static readonly Dictionary<string, System.Type> Routes = new()
        {
            ["Dashboard"]          = typeof(DashboardView),
            ["Assessments"]        = typeof(AssessmentsView),
            ["MarksEntry"]         = typeof(MarksEntryView),
            ["Gradebook"]          = typeof(GradebookView),
            ["StudentPerformance"] = typeof(StudentPerformanceView),
            ["Analytics"]          = typeof(AnalyticsView),
            ["Moderation"]         = typeof(ModerationView),
            ["ReportCards"]        = typeof(ReportCardsView),
            ["Students"]           = typeof(StudentsView),
            ["Classes"]            = typeof(ClassesView),
            ["AuditLog"]           = typeof(AuditLogView),
            ["FinDashboard"]       = typeof(FinancialsDashboardView),
            ["FeeCollection"]      = typeof(FeeCollectionView),
            ["Budget"]             = typeof(BudgetView),
            ["AiInsights"]         = typeof(AiInsightsView),
        };

        public ShellView()
        {
            InitializeComponent();
            _vm = new ShellViewModel();
            DataContext = _vm;
            Loaded += ShellView_Loaded;
            ThemeToggle.Toggled += ThemeToggle_Toggled;
        }

        private void ShellView_Loaded(object sender, RoutedEventArgs e)
        {
            var user = SessionService.Instance.CurrentUser;
            SidebarUserName.Text = user?.FullName ?? "User";
            SidebarUserRole.Text = _vm.UserRoleLabel;
            SidebarAvatar.DisplayName = user?.FullName ?? "U";
            HeaderUserName.Text = user?.FullName ?? "User";

            NavModeration.Visibility = _vm.IsAdministrator
                ? Visibility.Visible : Visibility.Collapsed;

            NavigationService.Instance.InitializeShell(ContentFrame);
            NavigateTo("Dashboard", NavDashboard);
        }

        private void NavItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag) return;
            NavigateTo(tag, btn);
        }

        private async void NavigateTo(string tag, Button btn)
        {
            if (!Routes.TryGetValue(tag, out var pageType)) return;

            // Update header
            if (PageMeta.TryGetValue(tag, out var meta))
            {
                PageTitleText.Text = meta.Title;
                PageSubtitleText.Text = meta.Subtitle;
            }

            // Active state: highlight active button
            SetActiveButton(btn);

            try
            {
                ContentFrame.Navigate(pageType);
            }
            catch (System.Exception ex)
            {
                // Surface navigation errors so they are visible during debugging/runtime
                try
                {
                    var dlg = new ContentDialog
                    {
                        Title = "Navigation error",
                        Content = ex.ToString(),
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dlg.ShowAsync();
                }
                catch { }
            }
        }

        private Brush GetThemeBrush(string resourceKey)
        {
            try
            {
                if (Application.Current is Application app)
                {
                    var themeKey = app.RequestedTheme == ApplicationTheme.Dark ? "Dark" : "Light";
                    if (app.Resources.ThemeDictionaries.TryGetValue(themeKey, out var dictObj) &&
                        dictObj is ResourceDictionary themeDict &&
                        themeDict.TryGetValue(resourceKey, out var brushObj) &&
                        brushObj is Brush brush)
                    {
                        return brush;
                    }
                }
            }
            catch { }

            return new SolidColorBrush(Colors.Transparent);
        }

        private void SetActiveButton(Button active)
        {
            // Reset previous
            if (_activeNavButton != null)
            {
                _activeNavButton.Background = new SolidColorBrush(Colors.Transparent);
                _activeNavButton.Foreground = GetThemeBrush("TextOnDarkBrush");
            }

            // Set new active
            active.Background = GetThemeBrush("NavyActiveBrush");
            active.Foreground = GetThemeBrush("TextOnDarkBrush");
            _activeNavButton = active;
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            SessionService.Instance.SignOut();
            NavigationService.Instance.Navigate(typeof(LoginView));
        }

        private void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (ThemeToggle.IsOn)
            {
                ThemeService.SetTheme(ApplicationTheme.Dark);
            }
            else
            {
                ThemeService.SetTheme(ApplicationTheme.Light);
            }
        }

        // Simple search result model used by the AutoSuggestBox
        private class SearchResult
        {
            public string Title { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // e.g., "Student" or "Class"
            public object? Payload { get; set; }
        }

        private async void TopSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
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
                }

                sender.ItemsSource = results;
            }
            catch
            {
                sender.ItemsSource = null;
            }
        }

        private async void TopSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
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
                    if (r.Type == "Student")
                        NavigationService.Instance.Navigate(typeof(Views.StudentsView));
                    else if (r.Type == "Class")
                        NavigationService.Instance.Navigate(typeof(Views.ClassesView));
                }
            }
            catch { }
        }
    }
}