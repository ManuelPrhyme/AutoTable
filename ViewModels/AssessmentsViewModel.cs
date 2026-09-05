using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class AssessmentsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _selectedClass = "All";
        [ObservableProperty] private string _selectedSubject = "All";
        [ObservableProperty] private string _selectedStream = "All";
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private Visibility _streamFilterVisibility = Visibility.Collapsed;

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<string> Streams { get; }
        public ObservableCollection<AssessmentItem> Assessments { get; }

        // Class Name → Id, so subject/stream options can be loaded per selected class.
        private readonly Dictionary<string, int> _classIds = new();

        public int TotalCount => Assessments.Count;
        public int PendingCount => Assessments.Count(a => a.MarksEnteredPercent < 100);
        public int VerifiedCount => Assessments.Count(a => a.IsVerified);
        public int PublishedCount => Assessments.Count(a => a.IsPublished);

        public bool IsAdministrator => SessionService.Instance.IsAdministrator;

        public AssessmentsViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            Classes = new ObservableCollection<string>(new[] { "All" });
            Subjects = new ObservableCollection<string>(new[] { "All" });
            Streams = new ObservableCollection<string>(new[] { "All" });
            Assessments = new ObservableCollection<AssessmentItem>();
            _ = LoadAsync();
        }

        partial void OnSelectedClassChanged(string value)
        {
            // Class change: reload both subject and stream options for the new class,
            // then reapply the filter. Stream and Subject selections are preserved
            // when they still exist in the new option lists.
            _ = OnClassChangedAsync();
        }
        partial void OnSelectedSubjectChanged(string value) => _ = ApplyFilterAsync();
        partial void OnSelectedStreamChanged(string value) => _ = ApplyFilterAsync();

        [RelayCommand]
        private async Task RefreshFilter()
        {
            await LoadAsync();
        }

        // ── ROLE-BASED GATING (dormant during development) ──────
        // Uncomment CanExecute when enforcing admin-only assessment creation:
        // [RelayCommand(CanExecute = nameof(IsAdministrator))]
        [RelayCommand]
        private void NewAssessment() => StatusMessage = "New Assessment dialog will open here (Admin).";

        private async Task LoadAsync()
        {
            var classes = await _dataService.GetClassesAsync();
            Classes.Clear();
            Classes.Add("All");
            _classIds.Clear();
            foreach (var c in classes)
            {
                Classes.Add(c.Name);
                _classIds[c.Name] = c.Id;
            }

            // Default subjects (used when "All" classes is selected).
            await ReloadSubjectOptionsAsync(allSubjects: true);
            await ReloadStreamOptionsAsync();

            await ApplyFilterAsync();
        }

        /// <summary>
        /// Restricts the Subject filter to the subjects assigned to the selected class
        /// (or all subjects when a class / "All" is picked). This mirrors the class-subject
        /// assignment made at class creation.
        /// </summary>
        private async Task ReloadSubjectOptionsAsync(bool allSubjects = false)
        {
            Subjects.Clear();
            Subjects.Add("All");

            if (!allSubjects && SelectedClass != "All" && _classIds.TryGetValue(SelectedClass, out var classId))
            {
                var subs = await _dataService.GetSubjectsForClassAsync(classId);
                foreach (var s in subs) Subjects.Add(s.Name);
                return;
            }

            var all = await _dataService.GetSubjectsAsync();
            foreach (var s in all) Subjects.Add(s.Name);
        }

        /// <summary>
        /// Populates the Stream filter with the selected class's streams (all streams when "All").
        /// Stream and Subject filters are independent — both are always available.
        /// </summary>
        private async Task ReloadStreamOptionsAsync()
        {
            Streams.Clear();
            Streams.Add("All");
            StreamFilterVisibility = Visibility.Visible;

            if (SelectedClass != "All" && _classIds.TryGetValue(SelectedClass, out var classId))
            {
                var streams = await _dataService.GetStreamsForClassAsync(classId);
                foreach (var st in streams) Streams.Add(st.Name);
            }
            else
            {
                var allStreams = await _dataService.GetAllStreamsAsync();
                foreach (var st in allStreams) Streams.Add(st.Name);
            }
        }

        /// <summary>Called when the Class filter changes: reloads subjects + streams, then filters.</summary>
        private async Task OnClassChangedAsync()
        {
            var prevSubject = SelectedSubject;
            var prevStream = SelectedStream;

            await ReloadSubjectOptionsAsync();
            await ReloadStreamOptionsAsync();

            // Preserve previous selections if they still exist in the new option lists.
            if (prevSubject != "All" && !Subjects.Contains(prevSubject))
                SelectedSubject = "All";
            if (prevStream != "All" && !Streams.Contains(prevStream))
                SelectedStream = "All";

            await ApplyFilterAsync();
        }

        private async Task ApplyFilterAsync()
        {
            var all = await _dataService.GetAssessmentsAsync();
            var filtered = all.Where(a =>
                (SelectedClass == "All" || a.ClassName == SelectedClass) &&
                (SelectedSubject == "All" || a.Subject == SelectedSubject) &&
                (SelectedStream == "All" || a.Scope != AssessmentScope.Stream ||
                 (a.StreamNames.Count > 0 && a.StreamNames.Contains(SelectedStream)) ||
                 a.StreamName == SelectedStream));

            Assessments.Clear();
            foreach (var item in filtered)
                Assessments.Add(item);

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(VerifiedCount));
            OnPropertyChanged(nameof(PublishedCount));
        }

        // Public helper to refresh aggregate counts after UI-side modifications
        public void RefreshCounts()
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(VerifiedCount));
            OnPropertyChanged(nameof(PublishedCount));
        }
    }
}