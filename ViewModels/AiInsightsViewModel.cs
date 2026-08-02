using AutoTable.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoTable.ViewModels
{
    public partial class AiInsightsViewModel : BaseViewModel
    {
        public ObservableCollection<string> AtRiskAlerts { get; } = new();
        public ObservableCollection<string> Recommendations { get; } = new();
        public ObservableCollection<AutomationItem> Automations { get; } = new();

        public AiInsightsViewModel() => Load();

        [RelayCommand]
        private void Refresh() => Load();

        private void Load()
        {
            AtRiskAlerts.Clear();
            AtRiskAlerts.Add("14 students in P6 are below 40% in Mathematics.");
            AtRiskAlerts.Add("P6 English average dropped 11% from last term.");
            AtRiskAlerts.Add("3 students in P3 have not submitted any marks.");
            AtRiskAlerts.Add("P2 Science completion is only 34% — deadline in 2 days.");

            Recommendations.Clear();
            Recommendations.Add("Recommend remedial sessions for P6 Mathematics cohort.");
            Recommendations.Add("Publish P4 results after moderation is complete.");
            Recommendations.Add("Marks entry for P3 Science is 92% — follow up with entrant.");
            Recommendations.Add("P5 Mid Term results are ready for report card generation.");

            Automations.Clear();
            Automations.Add(new AutomationItem { Name = "Generate At-Risk Report", Description = "Export PDF list of students below 40% average." });
            Automations.Add(new AutomationItem { Name = "Send Fee Reminders", Description = "Power Automate: email parents with outstanding balances." });
            Automations.Add(new AutomationItem { Name = "Bulk Print Report Cards", Description = "Queue all P5 report cards for batch printing." });
            Automations.Add(new AutomationItem { Name = "Sync to Google Sheets", Description = "Export gradebook data via Power Automate connector." });
        }
    }
}
