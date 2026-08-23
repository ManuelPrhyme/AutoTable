using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class AiInsightsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _statusMessage = string.Empty;

        public ObservableCollection<string> AtRiskAlerts { get; } = new();
        public ObservableCollection<string> Recommendations { get; } = new();
        public ObservableCollection<AutomationItem> Automations { get; } = new();

        public AiInsightsViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            _ = LoadAsync();
        }

        [RelayCommand]
        private void Refresh() => _ = LoadAsync();

        private async Task LoadAsync()
        {
            try
            {
                AtRiskAlerts.Clear();
                Recommendations.Clear();

                var classes = await _dataService.GetClassesAsync();
                var subjects = await _dataService.GetSubjectsAsync();

                // Derive at-risk alerts and remedial recommendations from live gradebook data.
                foreach (var cls in classes)
                {
                    foreach (var subj in subjects)
                    {
                        var rows = await _dataService.GetGradebookAsync(cls.Name, subj.Name);
                        if (rows.Count == 0) continue;

                        var atRisk = rows.Where(r => r.Average < 40).ToList();
                        if (atRisk.Count > 0)
                            AtRiskAlerts.Add($"{atRisk.Count} student(s) in {cls.Name} are below 40% in {subj.Name}.");

                        var classAvg = rows.Average(r => r.Average);
                        if (classAvg < 50)
                            Recommendations.Add($"Recommend remedial sessions for {cls.Name} {subj.Name} cohort (class average {classAvg:0.#}%).");
                    }
                }

                // Derive workflow recommendations from assessment lifecycle state.
                var assessments = await _dataService.GetAssessmentsAsync();
                foreach (var a in assessments.Where(a => !a.IsPublished))
                {
                    if (a.MarksEnteredPercent >= 100 && !a.IsVerified)
                        Recommendations.Add($"'{a.Name}' ({a.ClassName} {a.Subject}) is complete — ready for moderation.");
                    else if (a.MarksEnteredPercent < 100)
                        Recommendations.Add($"Marks entry for '{a.Name}' ({a.ClassName} {a.Subject}) is {a.MarksEnteredPercent}% — follow up with entrant.");
                }

                if (AtRiskAlerts.Count == 0)
                    AtRiskAlerts.Add("No at-risk students detected in current gradebook data.");
                if (Recommendations.Count == 0)
                    Recommendations.Add("No recommendations — all assessments are published and averages look healthy.");

                StatusMessage = $"Analyzed {classes.Count} class(es), {subjects.Count} subject(s), {assessments.Count} assessment(s) from the database.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to derive insights: " + ex.Message;
            }

            // Automation catalog is a static feature list, not DB-derived data.
            if (Automations.Count == 0)
            {
                Automations.Add(new AutomationItem { Name = "Generate At-Risk Report", Description = "Export PDF list of students below 40% average." });
                Automations.Add(new AutomationItem { Name = "Send Fee Reminders", Description = "Power Automate: email parents with outstanding balances." });
                Automations.Add(new AutomationItem { Name = "Bulk Print Report Cards", Description = "Queue all report cards for batch printing." });
                Automations.Add(new AutomationItem { Name = "Sync to Google Sheets", Description = "Export gradebook data via Power Automate connector." });
            }
        }
    }
}