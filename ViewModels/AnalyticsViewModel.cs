using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class AnalyticsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _selectedTerm = "Term 2, 2025";
        [ObservableProperty] private string _selectedSubject = "Mathematics";
        [ObservableProperty] private double _schoolAverage;
        [ObservableProperty] private string _bestClass = "-";
        [ObservableProperty] private string _needsAttentionClass = "-";
        [ObservableProperty] private string _trendLabel = "+4.3%";

        public ObservableCollection<string> Terms { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<ClassBreakdown> ClassBreakdown { get; } = new();

        public AnalyticsViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            Terms = new ObservableCollection<string>();
            Subjects = new ObservableCollection<string>();
            _ = InitializeAsync();
        }

        partial void OnSelectedTermChanged(string value) => _ = Load();
        partial void OnSelectedSubjectChanged(string value) => _ = Load();

        [RelayCommand]
        private async Task Refresh() => await Load();

        private async Task InitializeAsync()
        {
            var terms = await _dataService.GetTermsAsync();
            foreach (var t in terms) Terms.Add(t);

            var subjects = await _dataService.GetSubjectsAsync();
            foreach (var s in subjects) Subjects.Add(s.Name);

            await Load();
        }

        private async Task Load()
        {
            ClassBreakdown.Clear();
            double total = 0;

            var classes = await _dataService.GetClassesAsync();
            foreach (var cls in classes)
            {
                var rows = await _dataService.GetGradebookAsync(cls.Name, SelectedSubject, term: SelectedTerm);
                var avg = rows.Count == 0 ? 0 : rows.Average(r => r.Average);
                total += avg;
                ClassBreakdown.Add(new ClassBreakdown
                {
                    ClassName = cls.Name,
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