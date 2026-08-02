using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTable.ViewModels
{
    public partial class AnalyticsViewModel : BaseViewModel
    {
        [ObservableProperty] private string _selectedTerm = "Term 2, 2025";
        [ObservableProperty] private string _selectedSubject = "Mathematics";
        [ObservableProperty] private double _schoolAverage;
        [ObservableProperty] private string _bestClass = "P4";
        [ObservableProperty] private string _needsAttentionClass = "P6";
        [ObservableProperty] private string _trendLabel = "+4.3%";

        public ObservableCollection<string> Terms { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<ClassBreakdown> ClassBreakdown { get; } = new();

        public AnalyticsViewModel()
        {
            Terms = new ObservableCollection<string>(MockDataService.Instance.Terms);
            Subjects = new ObservableCollection<string>(MockDataService.Instance.Subjects);
            Load();
        }

        partial void OnSelectedTermChanged(string value) => Load();
        partial void OnSelectedSubjectChanged(string value) => Load();

        [RelayCommand]
        private void Refresh() => Load();

        private void Load()
        {
            ClassBreakdown.Clear();
            double total = 0;
            foreach (var cls in MockDataService.Instance.Classes)
            {
                var rows = MockDataService.Instance.GetGradebook(cls, SelectedSubject);
                var avg = rows.Count == 0 ? 0 : rows.Average(r => r.Average);
                total += avg;
                ClassBreakdown.Add(new ClassBreakdown
                {
                    ClassName = cls,
                    StudentCount = rows.Count,
                    Average = avg,
                    AtRisk = rows.Count(r => r.Average < 40),
                    Excellent = rows.Count(r => r.Average >= 80),
                    Trend = avg >= 65 ? "↑ Improving" : avg >= 50 ? "→ Stable" : "↓ Declining"
                });
            }
            SchoolAverage = ClassBreakdown.Count == 0 ? 0 : total / ClassBreakdown.Count;
            var best = ClassBreakdown.OrderByDescending(c => c.Average).FirstOrDefault();
            var worst = ClassBreakdown.OrderBy(c => c.Average).FirstOrDefault();
            BestClass = best?.ClassName ?? "-";
            NeedsAttentionClass = worst?.ClassName ?? "-";
        }
    }
}
