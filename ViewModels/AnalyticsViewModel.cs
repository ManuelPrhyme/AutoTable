using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class AnalyticsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _selectedTerm = "";
        [ObservableProperty] private string _selectedSubject = "";
        [ObservableProperty] private double _schoolAverage;
        [ObservableProperty] private string _bestClass = "-";
        [ObservableProperty] private string _needsAttentionClass = "-";
        [ObservableProperty] private string _trendLabel = "-";

        public ObservableCollection<string> Terms { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<ClassBreakdown> ClassBreakdown { get; } = new();

        public ObservableCollection<TermTrendPoint> TermTrend { get; } = new();

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

            if (Terms.Count > 0) SelectedTerm = Terms[0];
            if (Subjects.Count > 0) SelectedSubject = Subjects[0];

            await Load();
        }

        private async Task Load()
        {
            ClassBreakdown.Clear();
            TermTrend.Clear();
            double total = 0;

            var classes = await _dataService.GetClassesAsync();

            // Class breakdown (current term + subject)
            foreach (var cls in classes)
            {
                try
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
                        Trend = avg >= 65 ? "\u2191 Improving" : avg >= 50 ? "\u2192 Stable" : "\u2193 Declining"
                    });
                }
                catch { }
            }
            SchoolAverage = ClassBreakdown.Count == 0 ? 0 : total / ClassBreakdown.Count;
            var best = ClassBreakdown.OrderByDescending(c => c.Average).FirstOrDefault();
            var worst = ClassBreakdown.OrderBy(c => c.Average).FirstOrDefault();
            BestClass = best?.ClassName ?? "-";
            NeedsAttentionClass = worst?.ClassName ?? "-";

            // Term-over-term trend: compute school average per term
            foreach (var termName in Terms)
            {
                double termTotal = 0;
                int termClassCount = 0;
                foreach (var cls in classes)
                {
                    try
                    {
                        var rows = await _dataService.GetGradebookAsync(cls.Name, SelectedSubject, term: termName);
                        if (rows.Count > 0)
                        {
                            termTotal += rows.Average(r => r.Average);
                            termClassCount++;
                        }
                    }
                    catch { }
                }
                var termAvg = termClassCount == 0 ? 0 : termTotal / termClassCount;
                TermTrend.Add(new TermTrendPoint { Term = termName, Average = termAvg });
            }

            if (TermTrend.Count >= 2)
            {
                var last = TermTrend[^1].Average;
                var prev = TermTrend[^2].Average;
                var diff = last - prev;
                TrendLabel = diff >= 0 ? $"+{diff:F1}%" : $"{diff:F1}%";
            }
            else
            {
                TrendLabel = "-";
            }
        }
    }

    public class TermTrendPoint
    {
        public string Term { get; set; } = string.Empty;
        public double Average { get; set; }
    }
}
