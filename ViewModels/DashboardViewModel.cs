using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AutoTable.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        public string UserDisplayName { get; }
        public string UserRoleLabel { get; }
        public string SelectedTerm { get; }
        public bool IsAdministrator => SessionService.Instance.IsAdministrator;

        public ObservableCollection<KpiMetric> KpiMetrics { get; } = new();
        public ObservableCollection<string> QuickActions { get; } = new();
        public ObservableCollection<string> AiInsights { get; } = new();
        public ObservableCollection<string> RecentActivity { get; } = new();

        public DashboardViewModel()
        {
            var user = SessionService.Instance.CurrentUser;
            UserDisplayName = user?.FullName ?? "User";
            UserRoleLabel = user?.Role == UserRole.Administrator ? "Administrator" : "Data Entrant";
            SelectedTerm = "Term 2, 2025";

            LoadKpiMetrics();
            LoadQuickActions();
            LoadAiInsights();
            LoadRecentActivity();
        }

        private void LoadKpiMetrics()
        {
            KpiMetrics.Add(new KpiMetric { Title = "Students", Value = "1,248", Subtitle = "Across 24 classes", IconGlyph = "\uE716", AccentColor = "#007BFF" });
            KpiMetrics.Add(new KpiMetric { Title = "Subjects", Value = "18", Subtitle = "Active subjects", IconGlyph = "\uE82D", AccentColor = "#6C63FF" });
            KpiMetrics.Add(new KpiMetric { Title = "Assessments Pending", Value = "6", Subtitle = "Awaiting marks entry", IconGlyph = "\uE787", AccentColor = "#F59E0B" });
            KpiMetrics.Add(new KpiMetric { Title = "Marks Entered", Value = "86%", Subtitle = "Of all assessments", IconGlyph = "\uE73E", AccentColor = "#22C55E" });
            KpiMetrics.Add(new KpiMetric { Title = "Published Results", Value = "4", Subtitle = "Classes published", IconGlyph = "\uE8A5", AccentColor = "#007BFF" });
            KpiMetrics.Add(new KpiMetric { Title = "Average Score", Value = "67.4%", Subtitle = "Overall average", IconGlyph = "\uE9D2", AccentColor = "#6C63FF" });
            KpiMetrics.Add(new KpiMetric { Title = "Students At Risk", Value = "32", Subtitle = "Below 40% average", IconGlyph = "\uE7BA", AccentColor = "#EF4444" });
        }

        private void LoadQuickActions()
        {
            QuickActions.Add("Enter Marks");
            QuickActions.Add("Import Marks");
            QuickActions.Add("Generate Report Cards");
            if (IsAdministrator)
            {
                QuickActions.Add("Publish Results");
                QuickActions.Add("Moderate Marks");
            }
            QuickActions.Add("View Analytics");
        }

        private void LoadAiInsights()
        {
            AiInsights.Add("14 students are at risk of failing Mathematics.");
            AiInsights.Add("P6 English average dropped by 11% this term.");
            AiInsights.Add("Marks entry for P3 Science is 92% complete.");
            AiInsights.Add("Recommend publishing P4 results after moderation.");
        }

        private void LoadRecentActivity()
        {
            RecentActivity.Add("John M. entered marks for P4 Science.");
            RecentActivity.Add("Admin published P5 Mid Term results.");
            RecentActivity.Add("Grace K. imported CAT 2 marks for P6.");
            RecentActivity.Add("System generated report cards for P3.");
        }
    }
}
