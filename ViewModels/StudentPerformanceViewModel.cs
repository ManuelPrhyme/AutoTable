using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace AutoTable.ViewModels
{
    public partial class StudentPerformanceViewModel : BaseViewModel
    {
        // Query parameters — each capable of being null
        [ObservableProperty] private string? _selectedAcademicYear;
        [ObservableProperty] private string? _selectedTerm = "Term 2, 2025";
        [ObservableProperty] private string? _selectedClass = "P5";
        [ObservableProperty] private string? _selectedStream;
        [ObservableProperty] private string? _selectedStudent;
        [ObservableProperty] private string? _selectedSubject = "Mathematics";

        // Search bar text
        [ObservableProperty] private string? _searchText;

        // Collections
        public ObservableCollection<string> AcademicYears { get; }
        public ObservableCollection<string> Streams { get; }
        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<string> Terms { get; }
        public ObservableCollection<string> AllStudents { get; }
        public ObservableCollection<GradebookRow> Students { get; } = new();

        // Master list for search filtering
        private List<GradebookRow> _loadedStudents = new();

        // KPI metrics
        public int TotalStudents => Students.Count;
        public double ClassAverage => Students.Count == 0 ? 0 : Students.Average(s => s.Average);
        public int AtRiskCount => Students.Count(s => s.Average < 40);
        public int ExcellentCount => Students.Count(s => s.Average >= 80);

        // Modal request event
        public event EventHandler<StudentPerformanceModalViewModel>? RequestShowModal;

        public StudentPerformanceViewModel()
        {
            AcademicYears = new ObservableCollection<string>(MockDataService.Instance.AcademicYears);
            Streams = new ObservableCollection<string>(MockDataService.Instance.Streams);
            Classes = new ObservableCollection<string>(MockDataService.Instance.Classes);
            Subjects = new ObservableCollection<string>(MockDataService.Instance.Subjects);
            Terms = new ObservableCollection<string>(MockDataService.Instance.Terms);
            AllStudents = new ObservableCollection<string>(MockDataService.Instance.AllStudents);
            Load();

            // React to generated property changes (ObservableProperty source generator)
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedAcademicYear)
                    || e.PropertyName == nameof(SelectedTerm)
                    || e.PropertyName == nameof(SelectedClass)
                    || e.PropertyName == nameof(SelectedStream)
                    || e.PropertyName == nameof(SelectedStudent)
                    || e.PropertyName == nameof(SelectedSubject))
                {
                    Load();
                }

                if (e.PropertyName == nameof(SearchText))
                {
                    ApplySearchFilter();
                }
            };
        }


        [RelayCommand]
        private void Load()
        {
            var rows = MockDataService.Instance.GetGradebook(
                SelectedClass, SelectedSubject, SelectedAcademicYear, SelectedTerm, SelectedStream, SelectedStudent);
            _loadedStudents = rows.ToList();
            ApplySearchFilter();
            OnPropertyChanged(nameof(TotalStudents));
            OnPropertyChanged(nameof(ClassAverage));
            OnPropertyChanged(nameof(AtRiskCount));
            OnPropertyChanged(nameof(ExcellentCount));
        }

        private void ApplySearchFilter()
        {
            Students.Clear();
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _loadedStudents
                : _loadedStudents.Where(s => s.StudentName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            foreach (var r in filtered) Students.Add(r);
            OnPropertyChanged(nameof(TotalStudents));
            OnPropertyChanged(nameof(ClassAverage));
            OnPropertyChanged(nameof(AtRiskCount));
            OnPropertyChanged(nameof(ExcellentCount));
        }

        [RelayCommand]
        private void OpenStudentModal(GradebookRow student)
        {
            if (student == null) return;
            var modalVm = new StudentPerformanceModalViewModel(student);
            RequestShowModal?.Invoke(this, modalVm);
        }

        [RelayCommand]
        private void Export() { }
    }
}
