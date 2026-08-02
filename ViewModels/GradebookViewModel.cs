using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTable.ViewModels
{
    public partial class GradebookViewModel : BaseViewModel
    {
        [ObservableProperty] private string _selectedClass = "P5";
        [ObservableProperty] private string _selectedSubject = "Mathematics";
        [ObservableProperty] private string _statusMessage = string.Empty;

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<GradebookRow> GradebookRows { get; }

        public double ClassAverage => GradebookRows.Count == 0 ? 0 : GradebookRows.Average(r => r.Average);
        public int AtRiskCount => GradebookRows.Count(r => r.Average < 40);
        public int TopPerformers => GradebookRows.Count(r => r.Average >= 80);

        public GradebookViewModel()
        {
            Classes = new ObservableCollection<string>(MockDataService.Instance.Classes);
            Subjects = new ObservableCollection<string>(MockDataService.Instance.Subjects);
            GradebookRows = new ObservableCollection<GradebookRow>();
            LoadGradebook();
        }

        partial void OnSelectedClassChanged(string value) => LoadGradebook();
        partial void OnSelectedSubjectChanged(string value) => LoadGradebook();

        [RelayCommand]
        private void Refresh() => LoadGradebook();

        [RelayCommand]
        private void Export() => StatusMessage = "Export to Excel will run via Power Automate (placeholder).";

        private void LoadGradebook()
        {
            var rows = MockDataService.Instance.GetGradebook(SelectedClass, SelectedSubject);
            GradebookRows.Clear();
            foreach (var row in rows)
                GradebookRows.Add(row);

            OnPropertyChanged(nameof(ClassAverage));
            OnPropertyChanged(nameof(AtRiskCount));
            OnPropertyChanged(nameof(TopPerformers));
            StatusMessage = $"Showing gradebook for {SelectedClass} — {SelectedSubject}.";
        }
    }
}
