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
    public partial class DefaultersAnalyticsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        // ── Filters ──
        [ObservableProperty] private string _selectedTermName = string.Empty;
        [ObservableProperty] private string _selectedClassName = "All";
        [ObservableProperty] private decimal _minBalance = 0;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public ObservableCollection<string> TermNames { get; } = new();
        public ObservableCollection<string> ClassNames { get; } = new();
        private List<SimpleLookup> _termLookups = new();
        private List<SimpleLookup> _classLookups = new();
        private bool _suppressLoad;

        // ── KPI cards ──
        [ObservableProperty] private int _totalDefaulters;
        [ObservableProperty] private decimal _totalOwed;
        [ObservableProperty] private double _schoolCollectionRate;
        public string SchoolCollectionRateDisplay => $"{SchoolCollectionRate:F1}%";
        [ObservableProperty] private int _totalStudentsInScope;
        [ObservableProperty] private int _paidInScope;
        [ObservableProperty] private int _partialInScope;
        [ObservableProperty] private int _unpaidInScope;

        // ── Data ──
        public ObservableCollection<DefaulterRecord> Defaulters { get; } = new();
        public ObservableCollection<DefaulterRecord> TopDefaulters { get; } = new();
        public ObservableCollection<CohortSummary> CohortSummaries { get; } = new();

        public DefaultersAnalyticsViewModel()
        {
            _dataService = AppServices.DataService ?? throw new InvalidOperationException("DataService not configured.");
            _ = InitializeAsync();
        }

        private bool _initialized;

        partial void OnSelectedTermNameChanged(string value)
        {
            if (_initialized && !_suppressLoad) _ = LoadAsync();
        }

        partial void OnSelectedClassNameChanged(string value)
        {
            if (_initialized && !_suppressLoad) _ = LoadAsync();
        }

        partial void OnMinBalanceChanged(decimal value)
        {
            if (_initialized && !_suppressLoad) _ = LoadAsync();
        }

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();

        private async Task InitializeAsync()
        {
            _suppressLoad = true;

            // Load terms
            _termLookups = (await _dataService.GetTermLookupsAsync()).ToList();
            TermNames.Clear();
            TermNames.Add("All Terms");
            foreach (var t in _termLookups) TermNames.Add(t.Name);

            // Load classes
            _classLookups = (await _dataService.GetClassesAsync()).ToList();
            ClassNames.Clear();
            ClassNames.Add("All");
            foreach (var c in _classLookups) ClassNames.Add(c.Name);

            // Default selections
            var active = await _dataService.GetActiveTermAsync();
            if (active != null && TermNames.Contains(active.Name))
                SelectedTermName = active.Name;
            else if (TermNames.Count > 1)
                SelectedTermName = TermNames[1];
            else
                SelectedTermName = "All Terms";

            SelectedClassName = "All";

            _suppressLoad = false;
            _initialized = true;

            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                // Resolve filters
                bool showAllTerms = string.Equals(SelectedTermName, "All Terms", StringComparison.OrdinalIgnoreCase);
                var selectedTerm = showAllTerms ? null : _termLookups.FirstOrDefault(t => string.Equals(t.Name, SelectedTermName, StringComparison.OrdinalIgnoreCase));
                int? termId = selectedTerm?.Id;

                bool showAllClasses = string.Equals(SelectedClassName, "All", StringComparison.OrdinalIgnoreCase);
                var selectedClass = showAllClasses ? null : _classLookups.FirstOrDefault(c => string.Equals(c.Name, SelectedClassName, StringComparison.OrdinalIgnoreCase));
                int? classId = selectedClass?.Id;

                decimal minBal = MinBalance;

                // Load defaulters
                var defaulters = await _dataService.GetDefaultersAsync(termId, classId, minBal > 0 ? minBal : null);

                // Apply available credits to reduce balances
                if (termId.HasValue)
                {
                    foreach (var d in defaulters)
                    {
                        try
                        {
                            var credit = await _dataService.GetAvailableCreditAsync(d.StudentId, termId.Value);
                            if (credit > 0)
                            {
                                d.PaidAmount += (decimal)credit;
                                if (d.Balance <= 0) d.PaidAmount = d.ExpectedAmount; // clamp
                            }
                        }
                        catch { /* credits table may not exist yet */ }
                    }
                }

                Defaulters.Clear();
                foreach (var d in defaulters) Defaulters.Add(d);

                TopDefaulters.Clear();
                foreach (var d in defaulters.Where(d => d.Balance > 0).Take(10)) TopDefaulters.Add(d);

                // Load cohort summaries
                var cohorts = await _dataService.GetCohortSummariesAsync(termId);
                CohortSummaries.Clear();
                foreach (var c in cohorts) CohortSummaries.Add(c);

                // Compute KPIs
                var schoolWide = cohorts.FirstOrDefault(c => c.Label == "School-wide");
                TotalDefaulters = defaulters.Count(d => d.Balance > 0);
                TotalOwed = defaulters.Where(d => d.Balance > 0).Sum(d => d.Balance);
                SchoolCollectionRate = schoolWide?.CollectionRate ?? 0;
                TotalStudentsInScope = schoolWide?.TotalStudents ?? 0;
                PaidInScope = schoolWide?.PaidCount ?? 0;
                PartialInScope = schoolWide?.PartialCount ?? 0;
                UnpaidInScope = schoolWide?.UnpaidCount ?? 0;

                StatusMessage = showAllTerms && showAllClasses
                    ? $"Showing all terms, all classes — {defaulters.Count} student(s) with balance."
                    : $"Showing {SelectedTermName}, {SelectedClassName} — {defaulters.Count} student(s) with balance.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Failed to load analytics: " + ex.Message;
            }
        }
    }
}
