using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class MarksEntryViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _selectedClass = "P5";
        [ObservableProperty] private string _selectedSubject = "Mathematics";
        [ObservableProperty] private string _selectedAssessment = "Mid Term I";
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private int _completionPercent;

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<string> Assessments { get; }
        public ObservableCollection<StudentMarkRow> StudentMarks { get; }

        public bool IsAdministrator => SessionService.Instance.IsAdministrator;

        public MarksEntryViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            Classes = new ObservableCollection<string>();
            Subjects = new ObservableCollection<string>();
            Assessments = new ObservableCollection<string>();
            StudentMarks = new ObservableCollection<StudentMarkRow>();
            _ = InitializeAsync();
        }

        partial void OnSelectedClassChanged(string value) => _ = LoadMarks();
        partial void OnSelectedSubjectChanged(string value) => _ = LoadMarks();
        partial void OnSelectedAssessmentChanged(string value) => _ = LoadMarks();

        private async Task InitializeAsync()
        {
            var classes = await _dataService.GetClassesAsync();
            foreach (var c in classes) Classes.Add(c.Name);

            var subjects = await _dataService.GetSubjectsAsync();
            foreach (var s in subjects) Subjects.Add(s.Name);

            var all = await _dataService.GetAssessmentsAsync();
            Assessments.Clear();
            foreach (var a in all.Select(a => a.Name).Distinct())
                Assessments.Add(a);

            if (Assessments.Count > 0)
                SelectedAssessment = Assessments[0];

            await LoadMarks();
        }

        [RelayCommand]
        private async Task LoadMarks()
        {
            var rows = await _dataService.GetStudentMarksAsync(SelectedClass, SelectedSubject, SelectedAssessment);
            StudentMarks.Clear();
            foreach (var row in rows)
            {
                row.IsEditable = true;
                StudentMarks.Add(row);
            }
            UpdateCompletion();
            StatusMessage = $"Loaded {StudentMarks.Count} students for {SelectedClass} {SelectedSubject} — {SelectedAssessment}.";
        }

        [RelayCommand]
        private void SaveDraft()
        {
            UpdateCompletion();
            StatusMessage = $"Draft saved. {CompletionPercent}% of marks entered.";
        }

        [RelayCommand]
        private void SubmitMarks()
        {
            UpdateCompletion();
            if (CompletionPercent < 100)
            {
                StatusMessage = "Please enter all marks before submitting.";
                return;
            }
            StatusMessage = IsAdministrator
                ? "Marks submitted and marked for verification."
                : "Marks submitted for admin review.";
        }

        public void UpdateCompletion()
        {
            if (StudentMarks.Count == 0) { CompletionPercent = 0; return; }
            var entered = StudentMarks.Count(s => s.Mark.HasValue);
            CompletionPercent = (int)(entered * 100.0 / StudentMarks.Count);
        }
    }
}