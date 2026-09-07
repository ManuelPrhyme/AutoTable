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
    public partial class MarksEntryViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _selectedClass = "P5";
        [ObservableProperty] private string _selectedSubject = string.Empty;
        [ObservableProperty] private string _selectedAssessment = string.Empty;
        [ObservableProperty] private string _selectedStream = "All";
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private int _completionPercent;

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<string> Assessments { get; }
        public ObservableCollection<string> Streams { get; }
        public ObservableCollection<StudentMarkRow> StudentMarks { get; }

        public bool IsAdministrator => SessionService.Instance.IsAdministrator;

        // Cache of every assessment in the DB; the dropdown is filtered from this
        // by the selected class + subject so mismatched picks are impossible.
        private List<AssessmentItem> _allAssessments = new();
        private bool _initialized;

        public MarksEntryViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            Classes = new ObservableCollection<string>();
            Subjects = new ObservableCollection<string>();
            Assessments = new ObservableCollection<string>();
            Streams = new ObservableCollection<string>(new[] { "All" });
            StudentMarks = new ObservableCollection<StudentMarkRow>();
            _ = InitializeAsync();
        }

        // When a multi-subject assessment is selected the Subject filter "spins up"
        // so the user can choose which subject's marks they are entering for that
        // single shared assessment. Hidden for single-subject assessments.
        [ObservableProperty] private bool _isSubjectFilterVisible = true;
        private bool _suppressSubjectReload;

        public Microsoft.UI.Xaml.Visibility SubjectFilterVisibility =>
            IsSubjectFilterVisible ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        partial void OnIsSubjectFilterVisibleChanged(bool value) => OnPropertyChanged(nameof(SubjectFilterVisibility));

        // When a stream-scoped assessment is selected the Stream filter appears (before the
        // Subject filter) so the user can choose which stream of the paper they are marking.
        [ObservableProperty] private bool _isStreamFilterVisible = false;
        private bool _suppressStreamReload;

        public Microsoft.UI.Xaml.Visibility StreamFilterVisibility =>
            IsStreamFilterVisible ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        partial void OnIsStreamFilterVisibleChanged(bool value) => OnPropertyChanged(nameof(StreamFilterVisibility));

        partial void OnSelectedClassChanged(string value) => _ = ReloadForFiltersAsync();
        partial void OnSelectedSubjectChanged(string value)
        {
            // Programmatic subject assignments (filter spin-up) manage their own reload.
            if (_suppressSubjectReload) return;
            if (IsSubjectFilterVisible) _ = LoadMarks();
        }
        partial void OnSelectedAssessmentChanged(string value) => _ = OnAssessmentSelectionChangedAsync();
        partial void OnSelectedStreamChanged(string value)
        {
            if (_suppressStreamReload) return;
            if (IsStreamFilterVisible) _ = LoadMarks();
        }

        /// <summary>
        /// When the selected assessment changes: multi-subject assessments reveal the
        /// Subject filter (populated with the assessment's linked subjects) so the user
        /// picks the subject to enter marks for; single-subject assessments hide it and
        /// pin the subject to the assessment's own subject.
        /// </summary>
        private async Task OnAssessmentSelectionChangedAsync()
        {
            if (!_initialized) return;

            var match = FindAssessment();
            bool isMulti = match != null && match.SubjectNames.Count > 0;
            IsSubjectFilterVisible = isMulti;

            // Stream-scoped assessment: reveal the Stream filter (before Subject) populated
            // with the paper's target streams so the user can pick which stream to mark.
            bool isStreamScoped = match != null && match.Scope == AssessmentScope.Stream;
            if (isStreamScoped)
            {
                var prevStream = SelectedStream;
                Streams.Clear();
                Streams.Add("All");
                foreach (var sn in match!.StreamNames) Streams.Add(sn);
                IsStreamFilterVisible = true;
                _suppressStreamReload = true;
                SelectedStream = (Streams.Contains(prevStream) ? prevStream : (Streams.Count > 0 ? Streams[0] : "All"));
                _suppressStreamReload = false;
            }
            else
            {
                IsStreamFilterVisible = false;
                _suppressStreamReload = true;
                SelectedStream = "All";
                _suppressStreamReload = false;
            }

            if (isMulti)
            {
                var prev = SelectedSubject;
                Subjects.Clear();
                foreach (var s in match!.SubjectNames) Subjects.Add(s);

                _suppressSubjectReload = true;
                if (!string.IsNullOrEmpty(prev) && Subjects.Contains(prev)) SelectedSubject = prev;
                else if (Subjects.Count > 0) SelectedSubject = Subjects[0];
                else SelectedSubject = string.Empty;
                _suppressSubjectReload = false;

                await LoadMarks();
            }
            else if (match != null)
            {
                // Single-subject assessment: keep the Subjects list intact but pin the
                // selection to the assessment's own subject.
                var own = match.Subject?.Trim() ?? string.Empty;
                if (!string.Equals(SelectedSubject?.Trim(), own, StringComparison.OrdinalIgnoreCase))
                {
                    _suppressSubjectReload = true;
                    SelectedSubject = own;   // LoadMarks runs below
                    _suppressSubjectReload = false;
                }
                await LoadMarks();
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                var classes = await _dataService.GetClassesAsync();
                foreach (var c in classes) Classes.Add(c.Name);

                var subjects = await _dataService.GetSubjectsAsync();
                foreach (var s in subjects) Subjects.Add(s.Name);

                _allAssessments = (await _dataService.GetAssessmentsAsync()).ToList();

                // Default the filters to the first assessment's class so the
                // bar starts on a class that actually has an assessment. The subject
                // filter is (re)driven by OnAssessmentSelectionChangedAsync.
                if (_allAssessments.Count > 0)
                {
                    var first = _allAssessments[0];
                    if (Classes.Contains(first.ClassName)) SelectedClass = first.ClassName;
                }

                _initialized = true;
                RefreshAssessmentList();
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to load marks entry data: " + ex.Message;
            }
        }

        /// <summary>
        /// Rebuilds the Assessment dropdown so it only contains assessments that apply to
        /// the currently selected class (its own papers plus any school-wide paper). If
        /// none exist the list is cleared with a helpful message instead of letting the
        /// user pick something that can never be found in the database.
        /// </summary>
        private void RefreshAssessmentList()
        {
            var previous = SelectedAssessment;
            Assessments.Clear();

            // Assessments for the selected class, plus any school-wide paper
            // (ClassName "All Classes"), which applies to every class.
            var names = _allAssessments
                .Where(a => string.Equals(a.ClassName?.Trim(), SelectedClass?.Trim(), StringComparison.OrdinalIgnoreCase)
                         || string.Equals(a.ClassName?.Trim(), "All Classes", StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();

            foreach (var n in names) Assessments.Add(n);

            if (Assessments.Count == 0)
            {
                SelectedAssessment = string.Empty;
                StudentMarks.Clear();
                CompletionPercent = 0;
                StatusMessage = $"No assessments have been created for {SelectedClass} yet. Create one on the Assessments page first.";
                return;
            }

            if (!string.IsNullOrEmpty(previous) && Assessments.Contains(previous))
                SelectedAssessment = previous;   // keeps current selection; OnChanged reloads marks
            else
                SelectedAssessment = Assessments[0];
        }

        private async Task ReloadForFiltersAsync()
        {
            if (!_initialized) return;
            RefreshAssessmentList();          // empty combo sets its own message and returns
            if (!string.IsNullOrEmpty(SelectedAssessment))
                await LoadMarks();            // no-op when selection change already triggered it
        }

        [RelayCommand]
        private async Task LoadMarks()
        {
            // Nothing sensible to load when the combo is empty (no assessments exist
            // for the selected class + subject combination).
            if (string.IsNullOrWhiteSpace(SelectedAssessment))
            {
                StudentMarks.Clear();
                CompletionPercent = 0;
                return;
            }

            var rows = await _dataService.GetStudentMarksAsync(SelectedClass, SelectedSubject, SelectedAssessment, IsStreamFilterVisible ? SelectedStream : null);
            StudentMarks.Clear();
            foreach (var row in rows)
            {
                row.IsEditable = true;
                row.RefreshGrade(); // recompute grade for any mark already stored in the DB
                StudentMarks.Add(row);
            }
            UpdateCompletion();
            StatusMessage = $"Loaded {StudentMarks.Count} students for {SelectedClass} {SelectedSubject} - {SelectedAssessment}.";
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
                _ = AppServices.Audit.LogAsync("Marks", IsAdministrator ? "Submit" : "SubmitDraft", "Marks", null,
                    $"{SelectedClass} - {SelectedSubject}",
                    $"Submitted {enteredCount} mark(s) for assessment '{SelectedAssessment}' ({(IsAdministrator ? "verification" : "admin review")}).",
                    isSuccess: true);
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
                // Resolve the assessment Id from the cached list using case-insensitive,
                // trim-tolerant comparisons - avoids the exact-match DB lookup that used
                // to report "assessment not found" for assessments that clearly exist.
                var match = FindAssessment();
                if (match == null)
                {
                    // Maybe it was just created on the Assessments page after this page
                    // loaded - refresh the cache once and retry before giving up.
                    _allAssessments = (await _dataService.GetAssessmentsAsync()).ToList();
                    match = FindAssessment();
                }

                if (match == null || !int.TryParse(match.Id, out var assessmentId))
                {
                    StatusMessage = $"'{SelectedAssessment}' was not found for {SelectedClass} - {SelectedSubject}. " +
                                    "Create it on the Assessments page, then reload this page.";
                    return false;
                }

                var saved = 0;
                foreach (var row in StudentMarks)
                {
                    if (!row.Mark.HasValue) continue;
                    if (!int.TryParse(row.StudentId, out var studentId) || studentId == 0) continue;

                    row.RefreshGrade(); // keep the on-screen grade in sync with the saved mark
                    await _dataService.UpdateMarkAsync(
                        assessmentId,
                        studentId,
                        row.Mark.Value,
                        row.Grade == "-" ? GradeFromMark(row.Mark.Value) : row.Grade,
                        string.IsNullOrWhiteSpace(row.Remarks) ? null : row.Remarks,
                        // Multi-subject assessments store one mark per subject: the chosen
                        // subject is required so the mark lands in the right slot.
                        IsSubjectFilterVisible ? SelectedSubject : null);
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

        /// <summary>
        /// Finds the assessment matching the current assessment-name selection and class
        /// (case-insensitive, whitespace-tolerant) from the cached list. School-wide
        /// assessments (ClassName "All Classes") match any selected class. When several
        /// single-subject assessments share a name (legacy data), prefer the one whose
        /// subject matches the current subject selection.
        /// </summary>
        private AssessmentItem? FindAssessment()
        {
            var candidates = _allAssessments.Where(a =>
                string.Equals(a.Name?.Trim(), SelectedAssessment?.Trim(), StringComparison.OrdinalIgnoreCase)
             && (string.Equals(a.ClassName?.Trim(), SelectedClass?.Trim(), StringComparison.OrdinalIgnoreCase)
              || string.Equals(a.ClassName?.Trim(), "All Classes", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (candidates.Count <= 1) return candidates.FirstOrDefault();

            var subjectMatch = candidates.FirstOrDefault(a => a.SubjectNames.Count == 0 &&
                string.Equals(a.Subject?.Trim(), SelectedSubject?.Trim(), StringComparison.OrdinalIgnoreCase));
            return subjectMatch ?? candidates[0];
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