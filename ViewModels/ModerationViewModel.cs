using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class ModerationViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

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
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            Classes = new ObservableCollection<string>(new[] { "All" });
            Subjects = new ObservableCollection<string>(new[] { "All" });
            _ = Load();
        }

        [RelayCommand]
        private async Task Load()
        {
            if (Classes.Count == 1)
            {
                var classes = await _dataService.GetClassesAsync();
                foreach (var c in classes) Classes.Add(c.Name);
            }

            if (Subjects.Count == 1)
            {
                var subjects = await _dataService.GetSubjectsAsync();
                foreach (var s in subjects) Subjects.Add(s.Name);
            }

            ModerationItems.Clear();
            var all = await _dataService.GetAssessmentsAsync();
            foreach (var a in all)
            {
                if (SelectedClass != "All" && a.ClassName != SelectedClass) continue;
                if (SelectedSubject != "All" && a.Subject != SelectedSubject) continue;

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