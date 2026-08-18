using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;


namespace AutoTable.ViewModels
{
    public partial class StudentPerformanceViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

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
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            AcademicYears = new ObservableCollection<string>();
            Streams = new ObservableCollection<string>();
            Classes = new ObservableCollection<string>();
            Subjects = new ObservableCollection<string>();
            Terms = new ObservableCollection<string>();
            AllStudents = new ObservableCollection<string>();
            _ = LoadAsync();

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
                    _ = Load();
                }

                if (e.PropertyName == nameof(SearchText))
                {
                    ApplySearchFilter();
                }
            };
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (AcademicYears.Count == 0)
            {
                var years = await _dataService.GetAcademicYearsAsync();
                foreach (var y in years) AcademicYears.Add(y);
            }

            if (Streams.Count == 0)
            {
                var streams = await _dataService.GetStreamsAsync();
                foreach (var s in streams) Streams.Add(s);
            }

            if (Classes.Count == 0)
            {
                var classes = await _dataService.GetClassesAsync();
                foreach (var c in classes) Classes.Add(c.Name);
            }

            if (Subjects.Count == 0)
            {
                var subjects = await _dataService.GetSubjectsAsync();
                foreach (var s in subjects) Subjects.Add(s.Name);
            }

            if (Terms.Count == 0)
            {
                var terms = await _dataService.GetTermsAsync();
                foreach (var t in terms) Terms.Add(t);
            }

            if (AllStudents.Count == 0)
            {
                var students = await _dataService.GetAllStudentsAsync();
                foreach (var s in students) AllStudents.Add(s);
            }

            await Load();
        }

        private async Task Load()
        {
            var rows = await _dataService.GetGradebookAsync(
                SelectedClass ?? string.Empty,
                SelectedSubject ?? string.Empty,
                SelectedAcademicYear,
                SelectedTerm,
                SelectedStream,
                SelectedStudent);
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
        private async Task OpenStudentModal(GradebookRow student)
        {
            if (student == null) return;
            var modalVm = new StudentPerformanceModalViewModel(
                _dataService,
                student.StudentName,
                student.ClassName,
                student.Subject,
                student.AcademicYear,
                student.Term,
                student.Stream);
            await modalVm.InitializeAsync();
            RequestShowModal?.Invoke(this, modalVm);
        }

        [RelayCommand]
        private void Export() { }
    }
}
