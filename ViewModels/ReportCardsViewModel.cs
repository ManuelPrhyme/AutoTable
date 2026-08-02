using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AutoTable.ViewModels
{
    public partial class ReportCardsViewModel : BaseViewModel
    {
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
            Classes = new ObservableCollection<string>(MockDataService.Instance.Classes);
            Terms = new ObservableCollection<string>(MockDataService.Instance.Terms);
            Load();
        }

        partial void OnSelectedClassChanged(string value) => Load();

        private void Load()
        {
            ReportCards.Clear();
            var rows = MockDataService.Instance.GetGradebook(SelectedClass, "Mathematics");
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
