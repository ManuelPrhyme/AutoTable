using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class ReportCardsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _selectedClass = "P5";
        [ObservableProperty] private string _selectedTerm = "Term 2, 2025";

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Terms { get; }
        public ObservableCollection<ReportCardRow> ReportCards { get; } = new();

        public int TotalStudents => ReportCards.Count;
        public int GeneratedCount => ReportCards.Count;
        public int PrintedCount { get; private set; }

        public ReportCardsViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            Classes = new ObservableCollection<string>();
            Terms = new ObservableCollection<string>();
            _ = InitializeAsync();
        }

        partial void OnSelectedClassChanged(string value) => _ = Load();

        private async Task InitializeAsync()
        {
            var classes = await _dataService.GetClassesAsync();
            foreach (var c in classes) Classes.Add(c.Name);

            var terms = await _dataService.GetTermsAsync();
            foreach (var t in terms) Terms.Add(t);

            await Load();
        }

        private async Task Load()
        {
            ReportCards.Clear();
            var rows = await _dataService.GetGradebookAsync(SelectedClass, "Mathematics", term: SelectedTerm);
            foreach (var r in rows)
            {
                ReportCards.Add(new ReportCardRow
                {
                    Rank = r.Rank,
                    StudentName = r.StudentName,
                    AdmissionNumber = r.AdmissionNumber,
                    ClassName = r.ClassName,
                    Average = r.Average,
                    Status = r.Status
                });
            }
            OnPropertyChanged(nameof(TotalStudents));
            OnPropertyChanged(nameof(GeneratedCount));
        }

        [RelayCommand] private void GenerateAll() { }
        [RelayCommand] private void PrintAll() { PrintedCount = ReportCards.Count; OnPropertyChanged(nameof(PrintedCount)); }
    }
}