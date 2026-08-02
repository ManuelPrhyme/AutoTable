using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTable.ViewModels
{
    public partial class MarksEntryViewModel : BaseViewModel
    {
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
            Classes = new ObservableCollection<string>(MockDataService.Instance.Classes);
            Subjects = new ObservableCollection<string>(MockDataService.Instance.Subjects);
            Assessments = new ObservableCollection<string>(MockDataService.Instance.GetAssessments().Select(a => a.Name).Distinct());
            StudentMarks = new ObservableCollection<StudentMarkRow>();
            LoadMarks();
        }

        partial void OnSelectedClassChanged(string value) => LoadMarks();
        partial void OnSelectedSubjectChanged(string value) => LoadMarks();
        partial void OnSelectedAssessmentChanged(string value) => LoadMarks();

        [RelayCommand]
        private void LoadMarks()
        {
            var rows = MockDataService.Instance.GetStudentMarks(SelectedClass, SelectedSubject, SelectedAssessment);
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
