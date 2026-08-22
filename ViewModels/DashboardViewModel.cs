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
            LoadAiInsights();
            LoadRecentActivity();

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
            try
            {
                var students = await _dataService.GetStudentsAsync();
                var assessments = await _dataService.GetAssessmentsAsync();
                studentCount = students?.Count ?? 0;
                assessmentCount = assessments?.Count ?? 0;
            }
            catch
            {
                // Keep counters at zero if the data layer is unavailable.
            }

            KpiMetrics.Add(new KpiMetric { Title = "Total Students", Value = studentCount.ToString("N0"), Subtitle = "from database", IconGlyph = "\uE77B", AccentColor = "#007BFF" });
            KpiMetrics.Add(new KpiMetric { Title = "Avg Score", Value = "—", Subtitle = "—", IconGlyph = "\uE9D2", AccentColor = "#28A745" });
            KpiMetrics.Add(new KpiMetric { Title = "Assessments", Value = assessmentCount.ToString("N0"), Subtitle = "from database", IconGlyph = "\uE9F9", AccentColor = "#FD7E14" });
            KpiMetrics.Add(new KpiMetric { Title = "Attendance", Value = "—", Subtitle = "from database", IconGlyph = "\uE7E7", AccentColor = "#6610F2" });
            KpiMetrics.Add(new KpiMetric { Title = "Revenue", Value = "—", Subtitle = "from database", IconGlyph = "\uE929", AccentColor = "#20C997" });
        }

        private void LoadQuickActions()
        {
            QuickActions.Clear();
            QuickActions.Add("Create Term");
            QuickActions.Add("Enter Marks");
            QuickActions.Add("Add Assessment");
            QuickActions.Add("View Gradebook");
            QuickActions.Add("Generate Report Card");
            QuickActions.Add("Record Fees Payment");
        }

        private void LoadAiInsights()
        {
            AiInsights.Clear();
            AiInsights.Add("P6 English average dropped 8% — schedule extra revision.");
            AiInsights.Add("3 students at risk of failing Mathematics in P5.");
            AiInsights.Add("Attendance improved 4% after new late policy.");
            AiInsights.Add("Fee collection is 82% for Term 2 — 18% outstanding.");
        }

        private void LoadRecentActivity()
        {
            RecentActivity.Clear();
            RecentActivity.Add("Brian Okello's marks were updated by A. Namukasa");
            RecentActivity.Add("New assessment 'End Term' created for P6 English");
            RecentActivity.Add("Report cards published for P1 - Term 1");
            RecentActivity.Add("Grace Nabwire's fee payment of UGX 850,000 recorded");
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