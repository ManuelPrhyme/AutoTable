using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        [ObservableProperty] private string _statusMessage = string.Empty;

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<AssessmentItem> Assessments { get; }

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
            Assessments = new ObservableCollection<AssessmentItem>();
            _ = LoadAsync();
        }

        partial void OnSelectedClassChanged(string value) => _ = ApplyFilterAsync();
        partial void OnSelectedSubjectChanged(string value) => _ = ApplyFilterAsync();

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
            foreach (var c in classes) Classes.Add(c.Name);

            var subjects = await _dataService.GetSubjectsAsync();
            Subjects.Clear();
            Subjects.Add("All");
            foreach (var s in subjects) Subjects.Add(s.Name);

            await ApplyFilterAsync();
        }

        private async Task ApplyFilterAsync()
        {
            var all = await _dataService.GetAssessmentsAsync();
            var filtered = all.Where(a =>
                (SelectedClass == "All" || a.ClassName == SelectedClass) &&
                (SelectedSubject == "All" || a.Subject == SelectedSubject));

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