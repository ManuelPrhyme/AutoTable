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
    public partial class ReportCardsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        // --- Filter properties ---
        [ObservableProperty] private string _selectedClass = "All";
        [ObservableProperty] private string _selectedTerm = "All";
        [ObservableProperty] private string _selectedStream = null;
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _feeClearedOnly = false;

        // --- Collections ---
        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Terms { get; }
        public ObservableCollection<string> Streams { get; }
        public ObservableCollection<ReportCardRow> ReportCards { get; } = new();

        // --- KPI metrics ---
        public int TotalStudents => ReportCards.Count;
        public int GeneratedCount => ReportCards.Count;
        public int PrintedCount { get; private set; }

        // Master list for search filtering
        private List<ReportCardRow> _loadedRows = new();

        public ReportCardsViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            Classes = new ObservableCollection<string>();
            Terms = new ObservableCollection<string>();
            Streams = new ObservableCollection<string>();
            _ = InitializeAsync();
        }

        partial void OnSelectedClassChanged(string value) => _ = Load();
        partial void OnSelectedTermChanged(string value) => _ = Load();
        partial void OnSelectedStreamChanged(string value) => _ = Load();
        partial void OnSearchTextChanged(string value) => ApplySearchFilter();
        partial void OnFeeClearedOnlyChanged(bool value) => ApplySearchFilter();

        private async Task InitializeAsync()
        {
            Classes.Add("All");
            var classes = await _dataService.GetClassesAsync();
            foreach (var c in classes) Classes.Add(c.Name);

            Terms.Add("All");
            var terms = await _dataService.GetTermsAsync();
            foreach (var t in terms) Terms.Add(t);

            Streams.Add("All");
            var streams = await _dataService.GetStreamsAsync();
            foreach (var s in streams) Streams.Add(s);

            await Load();
        }

        private async Task Load()
        {
            ReportCards.Clear();
            _loadedRows.Clear();

            // Pass null for "All" selections so the service queries comprehensively
            var className = SelectedClass == "All" ? null : SelectedClass;
            var term = SelectedTerm == "All" ? null : SelectedTerm;
            var stream = SelectedStream;
            if (stream == "All" || stream == "None" || string.IsNullOrWhiteSpace(stream))
                stream = null;

            var rows = await _dataService.GetReportCardListAsync(
                className, term: term, stream: stream);

            foreach (var r in rows)
            {
                _loadedRows.Add(new ReportCardRow
                {
                    Rank = r.Rank,
                    StudentId = r.StudentId,
                    StudentName = r.StudentName,
                    AdmissionNumber = r.AdmissionNumber,
                    ClassName = r.ClassName,
                    Average = r.Average,
                    Status = r.Status,
                    FeeStatus = r.FeeStatus,
                    ExpectedAmount = r.ExpectedAmount,
                    PaidAmount = r.PaidAmount
                });
            }
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            ReportCards.Clear();

            string searchText = SearchText ?? "";
            bool hasSearch = !string.IsNullOrWhiteSpace(searchText);
            bool ignoreCase = true;

            StringComparison cmp = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            var filtered = _loadedRows.Where(row =>
            {
                // Finance filter: only show fee-cleared students
                if (FeeClearedOnly && row.FeeStatus != "Paid" && row.FeeStatus != "N/A")
                    return false;

                if (!hasSearch) return true;

                // Match by initials derived from first and last name,
                // or by a case-insensitive prefix of the full name / second name.
                string[] parts = row.StudentName.Split(
                    new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string firstName = parts.Length > 0 ? parts[0] : "";
                string lastName = parts.Length > 1 ? parts[parts.Length - 1] : "";
                string initials = string.Concat(
                    parts.Where(p => !string.IsNullOrEmpty(p))
                         .Select(p => p[0]));

                return row.StudentName.Contains(searchText, cmp)
                    || row.AdmissionNumber.Contains(searchText, cmp)
                    || initials.Contains(searchText, cmp)
                    || firstName.Contains(searchText, cmp)
                    || lastName.Contains(searchText, cmp);
            }).ToList();

            for (int i = 0; i < filtered.Count; i++)
                filtered[i].Rank = i + 1;

            foreach (var row in filtered)
                ReportCards.Add(row);
        }
    }
}