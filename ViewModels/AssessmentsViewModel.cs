using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTable.ViewModels
{
    public partial class AssessmentsViewModel : BaseViewModel
    {
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
            Classes = new ObservableCollection<string>(new[] { "All" }.Concat(MockDataService.Instance.Classes));
            Subjects = new ObservableCollection<string>(new[] { "All" }.Concat(MockDataService.Instance.Subjects));
            Assessments = new ObservableCollection<AssessmentItem>(MockDataService.Instance.GetAssessments());
        }

        partial void OnSelectedClassChanged(string value) => ApplyFilter();
        partial void OnSelectedSubjectChanged(string value) => ApplyFilter();

        [RelayCommand]
        private void RefreshFilter() => ApplyFilter();

        [RelayCommand(CanExecute = nameof(IsAdministrator))]
        private void NewAssessment() => StatusMessage = "New Assessment dialog will open here (Admin).";

        private void ApplyFilter()
        {
            var filtered = MockDataService.Instance.GetAssessments().Where(a =>
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
    }
}
