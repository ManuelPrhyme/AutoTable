using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class GradebookViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _selectedClass = "All";
        [ObservableProperty] private string _selectedSubject = "All";
        [ObservableProperty] private string _statusMessage = string.Empty;

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<GradebookRow> GradebookRows { get; }

        public double ClassAverage => GradebookRows.Count == 0 ? 0 : GradebookRows.Average(r => r.Average);
        public int AtRiskCount => GradebookRows.Count(r => r.Average < 40);
        public int TopPerformers => GradebookRows.Count(r => r.Average >= 80);

        public GradebookViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            Classes = new ObservableCollection<string>();
            Subjects = new ObservableCollection<string>();
            GradebookRows = new ObservableCollection<GradebookRow>();
            _ = LoadAsync();
        }

        partial void OnSelectedClassChanged(string value) => _ = LoadAsync();
        partial void OnSelectedSubjectChanged(string value) => _ = LoadAsync();

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();

        [RelayCommand]
        private void Export() => StatusMessage = "Export to Excel will run via Power Automate (placeholder).";

        private async Task LoadAsync()
        {
            if (Classes.Count == 0)
            {
                Classes.Add("All");
                var classes = await _dataService.GetClassesAsync();
                foreach (var c in classes) Classes.Add(c.Name);
            }

            if (Subjects.Count == 0)
            {
                Subjects.Add("All");
                var subjects = await _dataService.GetSubjectsAsync();
                foreach (var s in subjects) Subjects.Add(s.Name);
            }

            var className = SelectedClass == "All" ? null : SelectedClass;
            var subject = SelectedSubject == "All" ? null : SelectedSubject;

            var rows = await _dataService.GetGradebookAsync(className, subject);
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