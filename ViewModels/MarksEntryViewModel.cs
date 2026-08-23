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
        private async Task SaveDraft()
        {
            UpdateCompletion();
            var enteredCount = StudentMarks.Count(s => s.Mark.HasValue);
            if (enteredCount == 0)
            {
                StatusMessage = "No marks to save. Enter marks for students first.";
                return;
            }

            StatusMessage = $"Saving draft ({enteredCount} mark(s), {CompletionPercent}% complete)...";
            var saved = await PersistMarksAsync();
            if (saved)
            {
                StatusMessage = $"Draft saved: {enteredCount} mark(s) persisted. {CompletionPercent}% of marks entered.";
            }
        }

        [RelayCommand]
        private async Task SubmitMarks()
        {
            UpdateCompletion();

            if (StudentMarks.Count == 0)
            {
                StatusMessage = "No students loaded. Select a class, subject and assessment first.";
                return;
            }

            // Allow progressive/cumulative entry: submit whatever marks are entered.
            // Warn (but don't block) when less than 100% complete.
            var enteredCount = StudentMarks.Count(s => s.Mark.HasValue);
            if (enteredCount == 0)
            {
                StatusMessage = "No marks entered yet. Enter at least one mark before submitting.";
                return;
            }

            if (CompletionPercent < 100)
            {
                StatusMessage = $"Submitting {enteredCount} of {StudentMarks.Count} marks ({CompletionPercent}% complete). " +
                                "You can enter the remaining marks later.";
            }

            var saved = await PersistMarksAsync();
            if (saved)
            {
                StatusMessage = IsAdministrator
                    ? $"Submitted {enteredCount} mark(s) and marked for verification. {CompletionPercent}% complete."
                    : $"Submitted {enteredCount} mark(s) for admin review. {CompletionPercent}% complete.";
            }
        }

        /// <summary>
        /// Persists every entered mark to the database via IDataService.UpdateMarkAsync
        /// (which upserts the mark and recomputes the assessment's completion percent).
        /// </summary>
        private async Task<bool> PersistMarksAsync()
        {
            try
            {
                var assessment = await _dataService.GetAssessmentAsync(SelectedAssessment, SelectedClass, SelectedSubject);
                if (assessment == null || !int.TryParse(assessment.Id, out var assessmentId))
                {
                    StatusMessage = "Assessment not found in the database. Create it on the Assessments page first.";
                    return false;
                }

                var saved = 0;
                foreach (var row in StudentMarks)
                {
                    if (!row.Mark.HasValue) continue;
                    if (!int.TryParse(row.StudentId, out var studentId) || studentId == 0) continue;

                    await _dataService.UpdateMarkAsync(
                        assessmentId,
                        studentId,
                        row.Mark.Value,
                        GradeFromMark(row.Mark.Value),
                        string.IsNullOrWhiteSpace(row.Remarks) ? null : row.Remarks);
                    saved++;
                }

                UpdateCompletion();
                StatusMessage = $"Saved {saved} mark(s) to the database. {CompletionPercent}% of marks entered.";
                return true;
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to save marks: " + ex.Message;
                return false;
            }
        }

        public void UpdateCompletion()
        {
            if (StudentMarks.Count == 0) { CompletionPercent = 0; return; }
            var entered = StudentMarks.Count(s => s.Mark.HasValue);
            CompletionPercent = (int)(entered * 100.0 / StudentMarks.Count);
        }

        private static string GradeFromMark(double mark) => mark switch
        {
            >= 80 => "A",
            >= 70 => "B",
            >= 60 => "C",
            >= 50 => "D",
            >= 40 => "E",
            _ => "F"
        };
    }
}