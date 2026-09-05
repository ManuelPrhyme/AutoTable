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
    public partial class FinancialsDashboardViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        // ── Existing KPI cards ──
        [ObservableProperty] private decimal _totalCollected;
        [ObservableProperty] private decimal _outstandingFees;
        [ObservableProperty] private double _budgetUsedPercent;
        [ObservableProperty] private double _studentsPaidPercent;

        // ── Term-specific KPI cards (from Term Management) ──
        [ObservableProperty] private decimal _totalExpected;
        [ObservableProperty] private double _collectionRate;
        [ObservableProperty] private int _activeStudentCount;
        [ObservableProperty] private int _totalStudentCount;
        [ObservableProperty] private int _totalPaymentCount;
        [ObservableProperty] private int _partialCount;
        [ObservableProperty] private int _unpaidCount;

        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private string _selectedTermName = string.Empty;

        public ObservableCollection<string> TermNames { get; } = new();
        private List<SimpleLookup> _termLookups = new();
        private bool _suppressLoad;

        public ObservableCollection<Transaction> RecentTransactions { get; } = new();

        public FinancialsDashboardViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            _ = LoadAsync();
        }

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();

        partial void OnSelectedTermNameChanged(string value)
        {
            if (!_suppressLoad) _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                // Load terms for the selector
                _termLookups = (await _dataService.GetTermLookupsAsync()).ToList();
                if (TermNames.Count != _termLookups.Count + 1) // avoid re-adding on every refresh
                {
                    _suppressLoad = true;
                    TermNames.Clear();
                    TermNames.Add("All Terms");
                    foreach (var t in _termLookups) TermNames.Add(t.Name);
                    // Default to active term or first term
                    var active = await _dataService.GetActiveTermAsync();
                    if (active != null && TermNames.Contains(active.Name))
                        SelectedTermName = active.Name;
                    else if (TermNames.Count > 1)
                        SelectedTermName = TermNames[1];
                    else
                        SelectedTermName = "All Terms";
                    _suppressLoad = false;
                }

                // Resolve the selected term id (null = all terms)
                bool showAll = string.Equals(SelectedTermName, "All Terms", StringComparison.OrdinalIgnoreCase);
                var selectedLookup = showAll ? null : _termLookups.FirstOrDefault(t => string.Equals(t.Name, SelectedTermName, StringComparison.OrdinalIgnoreCase));
                int? termId = selectedLookup?.Id;

                // Actual collections from the FeePayments table, scoped to selected term
                var payments = await _dataService.GetFeePaymentsAsync(null, termId);
                TotalCollected = (decimal)payments.Sum(p => p.Amount);
                TotalPaymentCount = payments.Count;

                // Expected fees = configured per-class term fees × active students in each class
                var termFeesAll = await _dataService.GetTermFeesAsync();
                var students = (await _dataService.GetStudentsAsync()).Where(s => s.IsActive).ToList();
                TotalStudentCount = students.Count;

                // If a term is selected, only count term fees for that term; otherwise count all
                var termFees = termId.HasValue
                    ? termFeesAll.Where(tf => tf.TermId == termId.Value).ToList()
                    : termFeesAll.ToList();

                decimal expectedTotal = 0;
                foreach (var tf in termFees)
                {
                    var count = students.Count(s => s.ClassId == tf.ClassId);
                    expectedTotal += (decimal)(tf.Amount * count);
                }

                TotalExpected = expectedTotal;
                OutstandingFees = expectedTotal > TotalCollected ? expectedTotal - TotalCollected : 0;
                CollectionRate = expectedTotal == 0 ? 0 : Math.Round((double)(TotalCollected / expectedTotal * 100), 1);
                BudgetUsedPercent = expectedTotal == 0 ? 0 : Math.Round((double)(TotalCollected / expectedTotal * 100), 1);
                ActiveStudentCount = students.Count;

                // % of active students who have made at least one payment (scoped to term)
                var payingStudentIds = payments.Select(p => p.StudentId).Distinct().ToHashSet();
                var paidCount = students.Count(s => payingStudentIds.Contains(s.Id));
                StudentsPaidPercent = students.Count == 0 ? 0 : Math.Round(paidCount * 100.0 / students.Count, 1);

                // Counters for paid / partial / unpaid students (scoped to term)
                var allFeeRecords = new List<(int StudentId, decimal Expected, decimal Paid)>();
                foreach (var s in students)
                {
                    var sPayments = payments.Where(p => p.StudentId == s.Id).Sum(p => p.Amount);
                    var sExpected = termFees.FirstOrDefault(tf => tf.ClassId == s.ClassId)?.Amount ?? 0;
                    allFeeRecords.Add((s.Id, (decimal)sExpected, (decimal)sPayments));
                }
                PartialCount = allFeeRecords.Count(r => r.Paid > 0 && r.Paid < r.Expected);
                UnpaidCount = allFeeRecords.Count(r => r.Paid == 0 && r.Expected > 0);

                // Recent transactions from the database (latest first)
                RecentTransactions.Clear();
                foreach (var p in payments.Take(8))
                {
                    RecentTransactions.Add(new Transaction
                    {
                        StudentName = p.StudentName,
                        ClassName = p.ClassName,
                        Term = p.TermName,
                        Amount = (decimal)p.Amount,
                        PaymentDate = p.PaymentDate.ToString("dd MMM yyyy"),
                        Status = "Confirmed"
                    });
                }

                StatusMessage = termId.HasValue
                    ? $"Showing {SelectedTermName} — {payments.Count} payment(s), {students.Count} student(s)."
                    : $"Showing all terms — {payments.Count} payment(s), {students.Count} student(s).";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to load financials: " + ex.Message;
            }
        }
    }
}