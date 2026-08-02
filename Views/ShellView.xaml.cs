using AutoTable.Services;
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

        private void NavigateTo(string tag, Button btn)
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

            ContentFrame.Navigate(pageType);
        }

        private void SetActiveButton(Button active)
        {
            // Reset previous
            if (_activeNavButton != null)
            {
                _activeNavButton.Background = new SolidColorBrush(Colors.Transparent);
                _activeNavButton.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 241, 245, 249));
            }

            // Set new active
            active.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 58, 95)); // NavyActive
            active.Foreground = new SolidColorBrush(Colors.White);
            _activeNavButton = active;
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            SessionService.Instance.SignOut();
            NavigationService.Instance.Navigate(typeof(LoginView));
        }
    }
}
