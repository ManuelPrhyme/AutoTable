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
    public partial class AuditLogViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private List<TerminationLogItem> _allLogs = new();
        private List<EnrollmentFormData> _allEnrollments = new();

        public ObservableCollection<TerminationLogItem> FilteredLogs { get; } = new();
        public ObservableCollection<EnrollmentFormData> FilteredEnrollments { get; } = new();

        [ObservableProperty]
        private string filter = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string statusMessage = "Loading records...";

        public AuditLogViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            StatusMessage = "Loading records...";

            var logs = await _dataService.GetTerminationLogAsync();
            _allLogs = logs.ToList();

            var enrollments = await _dataService.GetEnrollmentsAsync();
            _allEnrollments = enrollments.ToList();

            ApplyFilter();
            IsBusy = false;
        }

        partial void OnFilterChanged(string value) => ApplyFilter();

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();

        private void ApplyFilter()
        {
            // Termination log filter
            FilteredLogs.Clear();
            var logQuery = string.IsNullOrWhiteSpace(Filter)
                ? _allLogs
                : _allLogs.Where(l =>
                    (l.StudentName?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (l.Reason?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false));

            foreach (var log in logQuery) FilteredLogs.Add(log);

            // Enrollments filter
            FilteredEnrollments.Clear();
            var enrollmentQuery = string.IsNullOrWhiteSpace(Filter)
                ? _allEnrollments
                : _allEnrollments.Where(e =>
                    (e.FullName?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.GuardianName?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.GuardianPhone?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.LIN?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false));

            foreach (var e in enrollmentQuery) FilteredEnrollments.Add(e);

            var records = FilteredLogs.Count + FilteredEnrollments.Count;
            StatusMessage = records == 0
                ? "No records found."
                : $"{FilteredLogs.Count} termination and {FilteredEnrollments.Count} enrollment record(s).";
        }
    }
}