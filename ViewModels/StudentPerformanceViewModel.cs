using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTable.ViewModels
{
    public partial class StudentPerformanceViewModel : BaseViewModel
    {
        [ObservableProperty] private string _selectedClass = "P5";
        [ObservableProperty] private string _selectedSubject = "Mathematics";
        [ObservableProperty] private string _selectedTerm = "Term 2, 2025";

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<string> Terms { get; }
        public ObservableCollection<GradebookRow> Students { get; } = new();

        public int TotalStudents => Students.Count;
        public double ClassAverage => Students.Count == 0 ? 0 : Students.Average(s => s.Average);
        public int AtRiskCount => Students.Count(s => s.Average < 40);
        public int ExcellentCount => Students.Count(s => s.Average >= 80);

        public StudentPerformanceViewModel()
        {
            Classes = new ObservableCollection<string>(MockDataService.Instance.Classes);
            Subjects = new ObservableCollection<string>(MockDataService.Instance.Subjects);
            Terms = new ObservableCollection<string>(MockDataService.Instance.Terms);
            Load();
        }

        partial void OnSelectedClassChanged(string value) => Load();
        partial void OnSelectedSubjectChanged(string value) => Load();

        [RelayCommand]
        private void Load()
        {
            var rows = MockDataService.Instance.GetGradebook(SelectedClass, SelectedSubject);
            Students.Clear();
            foreach (var r in rows) Students.Add(r);
            OnPropertyChanged(nameof(TotalStudents));
            OnPropertyChanged(nameof(ClassAverage));
            OnPropertyChanged(nameof(AtRiskCount));
            OnPropertyChanged(nameof(ExcellentCount));
        }

        [RelayCommand]
        private void Export() { }
    }
}
