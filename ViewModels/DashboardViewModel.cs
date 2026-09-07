using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        public string UserDisplayName { get; }
        public string UserRoleLabel { get; }
        public string SelectedTerm { get; }
        public bool IsAdministrator => SessionService.Instance.IsAdministrator;

        public ObservableCollection<KpiMetric> KpiMetrics { get; } = new();
        public ObservableCollection<string> QuickActions { get; } = new();
        public ObservableCollection<string> AiInsights { get; } = new();
        public ObservableCollection<string> RecentActivity { get; } = new();

        // Search-related properties
        [ObservableProperty]
        private string searchText = string.Empty;
        public ObservableCollection<string> SearchableItems { get; } = new();
        public ObservableCollection<string> FilteredAiInsights { get; set; } = new();
        public ObservableCollection<string> FilteredRecentActivity { get; set; } = new();

        public DashboardViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("Data service is not registered.");

            var user = SessionService.Instance.CurrentUser;
            UserDisplayName = user?.FullName ?? "User";
            UserRoleLabel = user?.Role == UserRole.Administrator ? "Administrator" : "Data Entrant";
            SelectedTerm = "Term 2, 2025";

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            await LoadKpiMetricsAsync();
            LoadQuickActions();
            await LoadAiInsightsAsync();
            await LoadRecentActivityAsync();

            // Initialize searchable items (combined list from all observable collections)
            InitializeSearchableItems();

            // Set up initial filtered collections
            UpdateFilteredCollections();
        }

        private async Task LoadKpiMetricsAsync()
        {
            KpiMetrics.Clear();

            int studentCount = 0;
            int assessmentCount = 0;
            double avgScore = 0;
            int gradebookRows = 0;
            decimal revenue = 0;
            try
            {
                var students = await _dataService.GetStudentsAsync();
                var assessments = await _dataService.GetAssessmentsAsync();
                studentCount = students?.Count(s => s.IsActive) ?? 0;
                assessmentCount = assessments?.Count ?? 0;

                // Compute average score across all classes and subjects
                var classes = await _dataService.GetClassesAsync();
                var subjects = await _dataService.GetSubjectsAsync();
                double totalAvg = 0;
                int classSubjectCount = 0;
                foreach (var cls in classes)
                {
                    foreach (var subj in subjects)
                    {
                        var rows = await _dataService.GetGradebookAsync(cls.Name, subj.Name);
                        if (rows.Count > 0)
                        {
                            totalAvg += rows.Average(r => r.Average);
                            classSubjectCount++;
                            gradebookRows += rows.Count;
                        }
                    }
                }
                if (classSubjectCount > 0) avgScore = totalAvg / classSubjectCount;

                // Revenue from fee payments — scoped to the active term only
                var activeTerm = await _dataService.GetActiveTermAsync();
                var payments = await _dataService.GetFeePaymentsAsync(null, activeTerm?.Id);
                revenue = (decimal)payments.Sum(p => p.Amount);
            }
            catch
            {
                // Keep counters at zero if the data layer is unavailable.
            }

            KpiMetrics.Add(new KpiMetric { Title = "Total Students", Value = studentCount.ToString("N0"), Subtitle = "registered", IconGlyph = "\uE77B", AccentColor = "#007BFF" });
            KpiMetrics.Add(new KpiMetric { Title = "Avg Score", Value = gradebookRows > 0 ? $"{avgScore:F1}%" : "—", Subtitle = gradebookRows > 0 ? $"across {gradebookRows} records" : "no marks entered", IconGlyph = "\uE9D2", AccentColor = "#28A745" });
            KpiMetrics.Add(new KpiMetric { Title = "Assessments", Value = assessmentCount.ToString("N0"), Subtitle = "created", IconGlyph = "\uE9F9", AccentColor = "#FD7E14" });
            KpiMetrics.Add(new KpiMetric { Title = "Revenue", Value = revenue > 0 ? $"UGX {revenue:N0}" : "—", Subtitle = revenue > 0 ? "collected" : "no payments recorded", IconGlyph = "\uE929", AccentColor = "#20C997" });
        }

        private void LoadQuickActions()
        {
            QuickActions.Clear();

            // Each quick action maps to a shell route tag. Only show the action if
            // the current user is allowed to open that route (admin-only routes and
            // invite-code AllowedPages restrictions are enforced by the same rule
            // set used at navigation time, so the buttons match real access).
            var actions = new (string Action, string RouteTag)[]
            {
                ("Create Term",          "TermManagement"),
                ("Enter Marks",          "MarksEntry"),
                ("Add Assessment",       "Assessments"),
                ("View Gradebook",       "Gradebook"),
                ("Generate Report Card", "ReportCards"),
                ("Record Fees Payment",  "FeeCollection"),
            };

            foreach (var (action, routeTag) in actions)
            {
                if (NavigationService.CurrentUserMayAccess(routeTag))
                    QuickActions.Add(action);
            }
        }

        private async Task LoadAiInsightsAsync()
        {
            AiInsights.Clear();
            try
            {
                var classes = await _dataService.GetClassesAsync();
                var subjects = await _dataService.GetSubjectsAsync();
                var assessments = await _dataService.GetAssessmentsAsync();

                // At-risk alerts from gradebook
                foreach (var cls in classes)
                {
                    foreach (var subj in subjects)
                    {
                        var rows = await _dataService.GetGradebookAsync(cls.Name, subj.Name);
                        if (rows.Count == 0) continue;

                        var atRisk = rows.Count(r => r.Average < 40);
                        if (atRisk > 0)
                            AiInsights.Add($"{atRisk} student(s) at risk in {cls.Name} {subj.Name} (below 40% average).");

                        var classAvg = rows.Average(r => r.Average);
                        if (classAvg < 50)
                            AiInsights.Add($"{cls.Name} {subj.Name} class average is {classAvg:F1}% — consider remedial sessions.");
                    }
                }

                // Assessment lifecycle recommendations

                var incomplete = assessments.Count(a => a.MarksEnteredPercent < 100);
                if (incomplete > 0)
                    AiInsights.Add($"{incomplete} assessment(s) still have marks pending entry.");

                // Fee collection status
                var payments = await _dataService.GetFeePaymentsAsync();
                var students = (await _dataService.GetStudentsAsync()).Where(s => s.IsActive).ToList();
                if (students.Count > 0)
                {
                    var payingStudents = payments.Select(p => p.StudentId).Distinct().Count();
                    var collectionRate = (double)payingStudents / students.Count * 100;
                    if (collectionRate < 100)
                        AiInsights.Add($"Fee collection is {collectionRate:F0}% — {students.Count - payingStudents} student(s) with no payments recorded.");
                }
            }
            catch
            {
                // Gracefully degrade — show no insights rather than crash
            }

            if (AiInsights.Count == 0)
                AiInsights.Add("No alerts — all indicators look healthy.");
        }

        private async Task LoadRecentActivityAsync()
        {
            RecentActivity.Clear();
            try
            {
                // Recent fee payments
                var payments = await _dataService.GetFeePaymentsAsync();
                foreach (var p in payments.Take(3))
                    RecentActivity.Add($"{p.StudentName} paid UGX {p.Amount:N0} ({p.TermName}) on {p.PaymentDate:dd MMM}");

                // Recent terminations
                var terminations = await _dataService.GetTerminationLogAsync();
                foreach (var t in terminations.Take(2))
                    RecentActivity.Add($"{t.StudentName} terminated ({t.Reason}) on {t.TerminationDate:dd MMM}");

                // Recent assessments (newest first by due date)
                var assessments = await _dataService.GetAssessmentsAsync();
                foreach (var a in assessments.OrderByDescending(a => a.DueDate).Take(2))
                    RecentActivity.Add($"Assessment '{a.Name}' created for {a.ClassName} {a.Subject} (due {a.DueDate:dd MMM})");
            }
            catch
            {
                // Gracefully degrade
            }

            if (RecentActivity.Count == 0)
                RecentActivity.Add("No recent activity recorded.");
        }

        private void InitializeSearchableItems()
        {
            // Add all AI Insights to searchable items
            foreach (var insight in AiInsights)
            {
                SearchableItems.Add(insight);
            }

            // Add all Recent Activity to searchable items
            foreach (var activity in RecentActivity)
            {
                SearchableItems.Add(activity);
            }
        }

        private void UpdateFilteredCollections()
        {
            // Filter AI Insights based on search text
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredAiInsights = new ObservableCollection<string>(AiInsights);
            }
            else
            {
                var lowerSearch = SearchText.ToLower();
                FilteredAiInsights = new ObservableCollection<string>(
                    AiInsights.Where(i => i.ToLower().Contains(lowerSearch))
                );
            }

            // Filter Recent Activity based on search text
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredRecentActivity = new ObservableCollection<string>(RecentActivity);
            }
            else
            {
                var lowerSearch = SearchText.ToLower();
                FilteredRecentActivity = new ObservableCollection<string>(
                    RecentActivity.Where(a => a.ToLower().Contains(lowerSearch))
                );
            }

            // Notify property changes
            OnPropertyChanged(nameof(FilteredAiInsights));
            OnPropertyChanged(nameof(FilteredRecentActivity));
        }

        partial void OnSearchTextChanged(string value)
        {
            UpdateFilteredCollections();
        }
    }
}