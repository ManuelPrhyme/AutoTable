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
        [ObservableProperty] private string _statusMessage = string.Empty;

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Subjects { get; }
        public ObservableCollection<ModerationItem> ModerationItems { get; } = new();

        public int PendingCount => ModerationItems.Count(m => m.Status == "Pending");
        public int ApprovedCount => ModerationItems.Count(m => m.Status == "Approved");
        public int RejectedCount => ModerationItems.Count(m => m.Status == "Rejected");

        public bool IsAdministrator => SessionService.Instance.IsAdministrator;

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
                    AssessmentId = int.TryParse(a.Id, out var id) ? id : 0,
                    AssessmentName = a.Name,
                    ClassName = a.ClassName,
                    Subject = a.Subject,
                    EntryCount = a.MarksEnteredPercent,
                    SubmittedBy = "Data Entrant",
                    Status = a.IsPublished ? "Published" : a.IsVerified ? "Approved" : a.MarksEnteredPercent >= 100 ? "Pending" : "Incomplete"
                });
            }
            RefreshCounts();
            StatusMessage = $"Loaded {ModerationItems.Count} assessment(s) for moderation.";
        }

        /// <summary>Persists verification for every pending assessment and updates the UI.</summary>
        [RelayCommand]
        private async Task ApproveAll()
        {
            var pending = ModerationItems.Where(m => m.Status == "Pending").ToList();
            if (pending.Count == 0)
            {
                StatusMessage = "No pending assessments to approve.";
                return;
            }

            try
            {
                foreach (var item in pending)
                {
                    await _dataService.VerifyAssessmentAsync(item.AssessmentId, true);
                    item.Status = "Approved";
                }
                StatusMessage = $"Approved {pending.Count} assessment(s) — saved to database.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to approve assessments: " + ex.Message;
            }
            RefreshCounts();
        }

        /// <summary>Persists publication for every approved assessment and updates the UI.</summary>
        [RelayCommand]
        private async Task Publish()
        {
            var approved = ModerationItems.Where(m => m.Status == "Approved").ToList();
            if (approved.Count == 0)
            {
                StatusMessage = "Nothing approved to publish yet. Approve assessments first.";
                return;
            }

            try
            {
                foreach (var item in approved)
                {
                    await _dataService.PublishAssessmentAsync(item.AssessmentId, true);
                    item.Status = "Published";
                }
                StatusMessage = $"Published {approved.Count} assessment(s) — saved to database.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to publish assessments: " + ex.Message;
            }
            RefreshCounts();
        }

        /// <summary>Approves a single assessment (persisted).</summary>
        public async Task ApproveItemAsync(ModerationItem item)
        {
            if (item.AssessmentId <= 0) return;
            try
            {
                await _dataService.VerifyAssessmentAsync(item.AssessmentId, true);
                item.Status = "Approved";
                StatusMessage = $"Approved '{item.AssessmentName}' — saved to database.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to approve: " + ex.Message;
            }
            RefreshCounts();
        }

        /// <summary>Rejects a single assessment (persisted as unverified/unpublished).</summary>
        public async Task RejectItemAsync(ModerationItem item)
        {
            if (item.AssessmentId <= 0) return;
            try
            {
                await _dataService.PublishAssessmentAsync(item.AssessmentId, false);
                await _dataService.VerifyAssessmentAsync(item.AssessmentId, false);
                item.Status = "Rejected";
                StatusMessage = $"Rejected '{item.AssessmentName}' — saved to database.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to reject: " + ex.Message;
            }
            RefreshCounts();
        }

        private void RefreshCounts()
        {
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(ApprovedCount));
            OnPropertyChanged(nameof(RejectedCount));
        }
    }
}