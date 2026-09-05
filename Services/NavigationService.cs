using AutoTable.Models;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoTable.Services
{
    public class NavigationService
    {
        private static NavigationService? _instance;
        public static NavigationService Instance => _instance ??= new NavigationService();

        private Frame? _rootFrame;
        private Frame? _shellFrame;

        /// <summary>
        /// Route tags only administrators may open. Centralized here so every
        /// navigation path (sidebar clicks and external NavigateToShellPage
        /// callers) enforces the same rule set.
        /// </summary>
        public static readonly HashSet<string> AdminOnlyRouteTags = new()
        {
            "Budget", "Promotion", "TermManagement", "AuditLog", "SchoolSettings"
        };

        /// <summary>
        /// True when the current session is allowed to open the given shell route:
        /// administrators may open everything; non-admins are blocked from
        /// AdminOnlyRouteTags and, when their account carries AllowedPages,
        /// from any page outside that list. Null/empty AllowedPages = full access.
        /// </summary>
        private static bool CurrentUserMayAccess(string tag)
        {
            var user = SessionService.Instance.CurrentUser;
            if (user == null) return true;
            if (user.Role == UserRole.Administrator) return true;
            if (AdminOnlyRouteTags.Contains(tag)) return false;
            if (string.IsNullOrWhiteSpace(user.AllowedPages)) return true;
            return user.AllowedPages
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(tag, StringComparer.OrdinalIgnoreCase);
        }

        public static IReadOnlyDictionary<string, Type> ShellRoutes { get; } = new Dictionary<string, Type>
        {
            // Performance
            ["Dashboard"]          = typeof(Views.DashboardView),
            ["Assessments"]        = typeof(Views.AssessmentsView),
            ["MarksEntry"]         = typeof(Views.MarksEntryView),
            ["Gradebook"]          = typeof(Views.GradebookView),
            ["StudentPerformance"] = typeof(Views.StudentPerformanceView),
            ["Analytics"]          = typeof(Views.AnalyticsView),

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
            ["SchoolSettings"]     = typeof(Views.SchoolSettingsView),
            ["AiInsights"]         = typeof(Views.AiInsightsView),
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

            // Defense-in-depth: refuse restricted navigation here, before the
            // frame switches, so callers (top search, quick actions, setup
            // flows) can never bypass role/AllowedPages gating.
            if (!CurrentUserMayAccess(tag)) return;

            _shellFrame.Navigate(pageType);
            ShellNavigated?.Invoke(tag);
        }

        public void NavigateToShell(Type viewType) => _shellFrame?.Navigate(viewType);
    }
}
