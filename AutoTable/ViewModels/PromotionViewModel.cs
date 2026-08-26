using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class PromotionViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private readonly SimpleLookup _allClassesItem = new() { Id = 0, Name = "All classes" };

        [ObservableProperty] private SimpleLookup? _selectedClass;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public ObservableCollection<SimpleLookup> Classes { get; } = new();
        public ObservableCollection<PromotionRow> Rows { get; } = new();

        public int SuggestedPromoteCount => Rows.Count(r => r.Suggested == PromotionStatus.Promoted);
        public int SuggestedRepeatCount => Rows.Count(r => r.Suggested == PromotionStatus.Repeat);
        public int PendingCount => Rows.Count(r => r.Status == PromotionStatus.Pending);

        public PromotionViewModel()
        {
            _dataService = AppServices.DataService ?? throw new InvalidOperationException("DataService not configured.");
            _ = LoadAsync();
        }

        partial void OnSelectedClassChanged(SimpleLookup? value) => _ = LoadAsync();

        private async Task LoadAsync()
        {
            try
            {
                if (Classes.Count == 0)
                {
                    Classes.Add(_allClassesItem);
                    foreach (var c in await _dataService.GetClassesAsync())
                        Classes.Add(new SimpleLookup { Id = c.Id, Name = c.Name });
                    SelectedClass = _allClassesItem;
                }

                int? classId = SelectedClass != null && SelectedClass.Id != 0 ? SelectedClass.Id : (int?)null;
                var rows = await _dataService.GetPromotionOverviewAsync(classId);

                Rows.Clear();
                foreach (var r in rows) Rows.Add(r);
                RefreshSummaries();

                StatusMessage = Rows.Count == 0
                    ? "No active students found. Enter Term 3 marks to populate the promotion overview."
                    : $"Loaded {Rows.Count} student(s). Suggested: {SuggestedPromoteCount} promote, {SuggestedRepeatCount} repeat.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading promotion overview: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ReloadAsync()
        {
            int? classId = SelectedClass != null && SelectedClass.Id != 0 ? SelectedClass.Id : (int?)null;
            var rows = await _dataService.GetPromotionOverviewAsync(classId);
            Rows.Clear();
            foreach (var r in rows) Rows.Add(r);
            RefreshSummaries();
        }

        private void RefreshSummaries()
        {
            OnPropertyChanged(nameof(SuggestedPromoteCount));
            OnPropertyChanged(nameof(SuggestedRepeatCount));
            OnPropertyChanged(nameof(PendingCount));
        }

        [RelayCommand]
        private async Task PromoteAsync(PromotionRow row)
        {
            if (row == null) return;
            try
            {
                await _dataService.PromoteStudentAsync(row.StudentId, row.TargetClassId);
                await ReloadAsync();
                StatusMessage = $"{row.StudentName} marked Promoted." +
                    (string.IsNullOrEmpty(row.TargetClassName) ? "" : $" -> {row.TargetClassName}");
            }
            catch (Exception ex) { StatusMessage = $"Unable to promote {row.StudentName}: {ex.Message}"; }
        }

        [RelayCommand]
        private async Task RepeatAsync(PromotionRow row)
        {
            if (row == null) return;
            try
            {
                await _dataService.RepeatStudentAsync(row.StudentId);
                await ReloadAsync();
                StatusMessage = $"{row.StudentName} will repeat {row.ClassName}.";
            }
            catch (Exception ex) { StatusMessage = $"Unable to mark repeat for {row.StudentName}: {ex.Message}"; }
        }

        [RelayCommand]
        private async Task ResetAsync(PromotionRow row)
        {
            if (row == null) return;
            try
            {
                await _dataService.ResetPromotionAsync(row.StudentId);
                await ReloadAsync();
                StatusMessage = $"{row.StudentName} reset to Pending.";
            }
            catch (Exception ex) { StatusMessage = $"Unable to reset {row.StudentName}: {ex.Message}"; }
        }

        [RelayCommand]
        private async Task ProcessAllAsync()
        {
            int? classId = SelectedClass != null && SelectedClass.Id != 0 ? SelectedClass.Id : (int?)null;
            try
            {
                var processed = await _dataService.ProcessAllPromotionsAsync(classId);
                await ReloadAsync();
                StatusMessage = processed == 0
                    ? "No pending students to process."
                    : $"Processed {processed} student(s): promoted those who passed, repeated those who didn't.";
            }
            catch (Exception ex) { StatusMessage = $"Error processing promotions: {ex.Message}"; }
        }

        [RelayCommand]
        private async Task ShiftAsync(PromotionRow row)
        {
            if (row == null) return;
            try
            {
                var classes = await _dataService.GetClassesAsync();
                var picker = new Microsoft.UI.Xaml.Controls.ComboBox
                {
                    Header = $"Move {row.StudentName} to class...",
                    Width = 280,
                    DisplayMemberPath = nameof(SimpleLookup.Name),
                    ItemsSource = classes
                };
                var initial = classes.FirstOrDefault(c => c.Id == row.TargetClassId);
                if (initial != null) picker.SelectedItem = initial;

                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Shift Student",
                    Content = picker,
                    PrimaryButtonText = "Move",
                    CloseButtonText = "Cancel",
                    XamlRoot = Microsoft.UI.Xaml.Window.Current?.Content?.XamlRoot
                };
                if (await dialog.ShowAsync() != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                    return;

                var target = picker.SelectedItem as SimpleLookup;
                if (target == null) return;
                await _dataService.ShiftStudentClassAsync(row.StudentId, target.Id);
                await ReloadAsync();
                StatusMessage = $"{row.StudentName} shifted to {target.Name}.";
            }
            catch (Exception ex) { StatusMessage = $"Unable to shift {row.StudentName}: {ex.Message}"; }
        }

    }
}
