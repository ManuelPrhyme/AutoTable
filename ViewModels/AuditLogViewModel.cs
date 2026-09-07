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
        private readonly AuditService _auditService;
        private List<AuditLogRow> _allRows = new();

        /// <summary>
        /// False until filter options have been populated for the first time.
        /// Prevents auto-reload from firing while the combos are being populated.
        /// </summary>
        private bool _filterOptionsLoaded;

        /// <summary>
        /// Rows displayed in the audit log table (newest first).
        /// </summary>
        public ObservableCollection<AuditLogRow> AuditRows { get; } = new();

        [ObservableProperty]
        private string filter = string.Empty;

        [ObservableProperty]
        private string? selectedCategory;

        [ObservableProperty]
        private string? selectedOperation;

        [ObservableProperty]
        private string? selectedUser;

        /// <summary>Optional range start (From picker). Null = no lower bound.</summary>
        [ObservableProperty]
        private DateTimeOffset? startDate;

        /// <summary>Optional range end (To picker). Null = no upper bound.</summary>
        [ObservableProperty]
        private DateTimeOffset? endDate;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string statusMessage = "Loading records...";

        /// <summary>Distinct audit categories, populated from the service.</summary>
        public ObservableCollection<string> Categories { get; } = new();

        /// <summary>Distinct audit operations, populated from the service.</summary>
        public ObservableCollection<string> Operations { get; } = new();

        /// <summary>Distinct user names (display), populated from the service.</summary>
        public ObservableCollection<string> Users { get; } = new();

        /// <summary>"All", "Success", "Failure" options for the status filter combo.</summary>
        public ObservableCollection<string> StatusOptions { get; } = new()
        {
            "All",
            "Success",
            "Failure"
        };

        private string _selectedStatusOption = "All";
        public string SelectedStatusOption
        {
            get => _selectedStatusOption;
            set
            {
                if (SetProperty(ref _selectedStatusOption, value) && _filterOptionsLoaded)
                    _ = LoadAsync();
            }
        }

        /// <summary>Number of entries loaded in the current result set.</summary>
        public int TotalOperations => _allRows.Count;

        /// <summary>Entries logged today (UTC).</summary>
        public int OperationsToday => _allRows.Count(r =>
            DateTime.TryParse(r.Entry.Timestamp, out var dt) && dt.Date == DateTime.UtcNow.Date);

        /// <summary>Entries that recorded a failed operation.</summary>
        public int FailureCount => _allRows.Count(r => r.Entry.IsSuccess == 0);

        /// <summary>Distinct users that produced the current result set.</summary>
        public int ActiveUsers => _allRows.Select(r => r.Entry.UserId).Distinct().Count();

        public AuditLogViewModel()
        {
            _auditService = AuditService.Instance;
        }

        /// <summary>
        /// Loads audit entries from the AuditService, newest first,
        /// using the CurrentCategory / SelectedOperation / SelectedUser /
        /// SelectedStatusOption / Filter properties for filtering.
        /// </summary>
        /// <param name="limit">Maximum entries to retrieve.</param>
        public async Task LoadAsync(int limit = 150)
        {
            IsBusy = true;
            StatusMessage = "Loading audit records...";

            try
            {
                int? userId = null;
                var selectedUser = NormalizeFilter(SelectedUser);
                if (selectedUser != null)
                {
                    var users = await _auditService.GetUsersAsync();
                    var match = users.FirstOrDefault(u => u.UserName == selectedUser);
                    if (match.UserId > 0)
                        userId = match.UserId;
                }

                bool? isSuccess = null;
                if (SelectedStatusOption == "Success") isSuccess = true;
                if (SelectedStatusOption == "Failure") isSuccess = false;

                var entries = await _auditService.GetEntriesAsync(
                    category: NormalizeFilter(SelectedCategory),
                    operation: NormalizeFilter(SelectedOperation),
                    userId: userId,
                    isSuccess: isSuccess,
                    startDate: StartDate?.UtcDateTime.Date,
                    endDate: EndDate?.UtcDateTime.Date.AddDays(1),
                    search: string.IsNullOrWhiteSpace(Filter) ? null : Filter,
                    limit: limit);

                // Newest entries first.
                entries = entries
                    .OrderByDescending(e => DateTime.TryParse(e.Timestamp, out var dt) ? dt : DateTime.MinValue)
                    .ToList();

                _allRows = entries.Select(e => new AuditLogRow(e)).ToList();

                AuditRows.Clear();
                foreach (var row in _allRows)
                    AuditRows.Add(row);

                StatusMessage = AuditRows.Count == 0
                    ? "No audit log entries found."
                    : $"{AuditRows.Count} audit log entry/entries loaded.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load audit records: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Populates the Category / Operation / User filter dropdown collections
        /// from the AuditService. Called once on first load.
        /// </summary>
        public async Task LoadFilterOptionsAsync()
        {
            try
            {
                var cats = await _auditService.GetCategoriesAsync();
                Categories.Clear();
                Categories.Add("All");
                foreach (var c in cats) Categories.Add(c);

                var ops = await _auditService.GetOperationsAsync();
                Operations.Clear();
                Operations.Add("All");
                foreach (var o in ops) Operations.Add(o);

                var users = await _auditService.GetUsersAsync();
                Users.Clear();
                Users.Add("All");
                foreach (var u in users)
                    Users.Add(u.UserName);

                _filterOptionsLoaded = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load filter options: {ex.Message}";
            }
        }

        /// <summary>Maps the "All" combo option (or empty) to a null service filter.</summary>
        private static string? NormalizeFilter(string? value) =>
            string.IsNullOrWhiteSpace(value) || value == "All" ? null : value;

        partial void OnFilterChanged(string value)
        {
            // Live search: PropertyChanged on the box re-queries across all columns.
            if (_filterOptionsLoaded) _ = LoadAsync();
        }

        // Auto-apply: changing any dropdown immediately reloads the table.
        partial void OnSelectedCategoryChanged(string? value)
        {
            if (_filterOptionsLoaded) _ = LoadAsync();
        }

        partial void OnSelectedOperationChanged(string? value)
        {
            if (_filterOptionsLoaded) _ = LoadAsync();
        }

        partial void OnSelectedUserChanged(string? value)
        {
            if (_filterOptionsLoaded) _ = LoadAsync();
        }

        partial void OnStartDateChanged(DateTimeOffset? value)
        {
            if (_filterOptionsLoaded) _ = LoadAsync();
        }

        partial void OnEndDateChanged(DateTimeOffset? value)
        {
            if (_filterOptionsLoaded) _ = LoadAsync();
        }
        [RelayCommand]
        private async Task Refresh()
        {
            await LoadFilterOptionsAsync();
            await LoadAsync();
        }

        /// <summary>Applies the current filter selections and reloads entries.</summary>
        [RelayCommand]
        private async Task ApplyFilter() => await LoadAsync();
    }
}
