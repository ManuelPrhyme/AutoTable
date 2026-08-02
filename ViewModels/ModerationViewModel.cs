using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTable.ViewModels
{
    public partial class ModerationViewModel : BaseViewModel
    {
        [ObservableProperty] private string _selectedClass = "All";
        [ObservableProperty] private string _selectedSubject = "All";

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<ModerationItem> ModerationItems { get; } = new();

        public int PendingCount => ModerationItems.Count(m => m.Status == "Pending");
        public int ApprovedCount => ModerationItems.Count(m => m.Status == "Approved");
        public int RejectedCount => ModerationItems.Count(m => m.Status == "Rejected");

        public ModerationViewModel()
        {
            Classes = new ObservableCollection<string>(new[] { "All" }.Concat(MockDataService.Instance.Classes));
            Subjects = new ObservableCollection<string>(new[] { "All" }.Concat(MockDataService.Instance.Subjects));
            Load();
        }

        [RelayCommand]
        private void Load()
        {
            ModerationItems.Clear();
            foreach (var a in MockDataService.Instance.GetAssessments())
            {
                ModerationItems.Add(new ModerationItem
                {
                    AssessmentName = a.Name,
                    ClassName = a.ClassName,
                    Subject = a.Subject,
                    EntryCount = a.MarksEnteredPercent,
                    SubmittedBy = "Data Entrant",
                    Status = a.IsVerified ? "Approved" : a.MarksEnteredPercent >= 100 ? "Pending" : "Incomplete"
                });
            }
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(ApprovedCount));
            OnPropertyChanged(nameof(RejectedCount));
        }

        [RelayCommand]
        private void ApproveAll()
        {
            foreach (var item in ModerationItems.Where(m => m.Status == "Pending"))
                item.Status = "Approved";
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(ApprovedCount));
        }

        [RelayCommand]
        private void Publish() { }
    }
}
