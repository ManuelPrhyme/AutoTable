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
            ["Budget"]             = typeof(Views.BudgetView),
        };

        public void Initialize(Frame rootFrame) => _rootFrame = rootFrame;
        public void InitializeShell(Frame shellFrame) => _shellFrame = shellFrame;

        public void Navigate(Type viewType) => _rootFrame?.Navigate(viewType);
        public void Navigate<T>() where T : class => Navigate(typeof(T));

        public void NavigateToShellPage(string tag)
        {
            if (_shellFrame == null || !ShellRoutes.TryGetValue(tag, out var pageType)) return;
            _shellFrame.Navigate(pageType);
        }

        public void NavigateToShell(Type viewType) => _shellFrame?.Navigate(viewType);
    }
}
