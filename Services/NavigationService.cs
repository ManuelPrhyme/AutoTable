using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace AutoTable.Services
{
    public class NavigationService
    {
        private static NavigationService? _instance;
        public static NavigationService Instance => _instance ??= new NavigationService();

        private Frame? _rootFrame;
        private Frame? _shellFrame;

        public static IReadOnlyDictionary<string, Type> ShellRoutes { get; } = new Dictionary<string, Type>
        {
            // Performance
            ["Dashboard"]          = typeof(Views.DashboardView),
            ["Assessments"]        = typeof(Views.AssessmentsView),
            ["MarksEntry"]         = typeof(Views.MarksEntryView),
            ["Gradebook"]          = typeof(Views.GradebookView),
            ["StudentPerformance"] = typeof(Views.StudentPerformanceView),
            ["Analytics"]          = typeof(Views.AnalyticsView),
            ["Moderation"]         = typeof(Views.ModerationView),
            ["ReportCards"]        = typeof(Views.ReportCardsView),
            ["AiInsights"]         = typeof(Views.AiInsightsView),
            // Financials
            ["FinDashboard"]       = typeof(Views.FinancialsDashboardView),
            ["FeeCollection"]      = typeof(Views.FeeCollectionView),
            ["Defaulters"]         = typeof(Views.DefaultersAnalyticsView),
            ["Budget"]             = typeof(Views.BudgetView),
            // Administration
            ["Students"]           = typeof(Views.StudentsView),
            ["Teachers"]           = typeof(Views.TeachersView),
            ["TermManagement"]     = typeof(Views.TermManagementView),
            ["Classes"]            = typeof(Views.ClassesView),
            ["AuditLog"]           = typeof(Views.AuditLogView),
        };

        /// <summary>
        /// Raised after a shell-frame navigation made through NavigateToShellPage.
        /// ShellView listens to this to keep the page header and sidebar highlight in sync.
        /// </summary>
        public event Action<string>? ShellNavigated;

        public void Initialize(Frame rootFrame) => _rootFrame = rootFrame;
        public void InitializeShell(Frame shellFrame) => _shellFrame = shellFrame;

        public void Navigate(Type viewType) => _rootFrame?.Navigate(viewType);
        public void Navigate<T>() where T : class => Navigate(typeof(T));

        public void NavigateToShellPage(string tag)
        {
            if (_shellFrame == null || !ShellRoutes.TryGetValue(tag, out var pageType)) return;
            _shellFrame.Navigate(pageType);
            ShellNavigated?.Invoke(tag);
        }

        public void NavigateToShell(Type viewType) => _shellFrame?.Navigate(viewType);
    }
}
