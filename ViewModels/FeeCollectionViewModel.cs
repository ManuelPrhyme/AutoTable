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
    public partial class FeeCollectionViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _selectedClass = "All";
        [ObservableProperty] private string _selectedTerm = "All";
        [ObservableProperty] private string _selectedStatus = "All";
        [ObservableProperty] private string _statusMessage = string.Empty;

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Terms { get; }
        public ObservableCollection<string> StatusOptions { get; } = new() { "All", "Paid", "Partial", "Unpaid" };
        public ObservableCollection<FeeRecord> FeeRecords { get; } = new();

        public decimal TotalExpected => FeeRecords.Sum(f => f.ExpectedAmount);
        public decimal TotalCollected => FeeRecords.Sum(f => f.PaidAmount);
        public decimal TotalOutstanding => FeeRecords.Sum(f => f.Balance);
        public double CollectionRate => TotalExpected == 0 ? 0 : (double)(TotalCollected / TotalExpected * 100);

        public FeeCollectionViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            Classes = new ObservableCollection<string>();
            Terms = new ObservableCollection<string>();
            _ = InitializeAsync();
        }

        // Suppress auto-load while InitializeAsync populates the lists
        private bool _suppressLoad;

        partial void OnSelectedClassChanged(string value) { if (!_suppressLoad) _ = Load(); }
        partial void OnSelectedTermChanged(string value) { if (!_suppressLoad) _ = Load(); }

        [RelayCommand]
        private async Task Refresh() => await Load();

        // NOTE: payment recording lives in FeeCollectionView.RecordPayment_Click (code-behind)
        // which opens the searchable Record Payment modal and calls IDataService.CreateFeePaymentAsync.

        private async Task InitializeAsync()
        {
            _suppressLoad = true;
            Classes.Clear();
            Classes.Add("All");
            var classes = await _dataService.GetClassesAsync();
            foreach (var c in classes) Classes.Add(c.Name);

            Terms.Clear();
            Terms.Add("All");
            var terms = await _dataService.GetTermsAsync();
            foreach (var t in terms) Terms.Add(t);

            // Default to the active term if one exists
            var activeTerm = await _dataService.GetActiveTermAsync();
            if (activeTerm != null && Terms.Contains(activeTerm.Name))
                SelectedTerm = activeTerm.Name;
            else if (Terms.Count > 1)
                SelectedTerm = Terms[1];

            if (Classes.Count > 1)
                SelectedClass = Classes[1];

            _suppressLoad = false;
            await Load();
        }

        /// <summary>
        /// Builds fee records from real data: expected amount from the per-class term-fee
        /// configuration and paid amounts from actual FeePayments rows in the database.
        /// </summary>
        private async Task Load()
        {
            FeeRecords.Clear();
            var errors = new System.Collections.Generic.List<string>();

            // Resolve class filter ("All" = no filter)
            AutoTable.Models.SimpleLookup? cls = null;
            bool showAllClasses = string.Equals(SelectedClass, "All", StringComparison.OrdinalIgnoreCase);
            try
            {
                var classes = await _dataService.GetClassesAsync();
                if (!showAllClasses)
                    cls = classes.FirstOrDefault(c => string.Equals(c.Name, SelectedClass, StringComparison.OrdinalIgnoreCase));
            }
            catch (System.Exception ex) { errors.Add("classes: " + ex.Message); }

            // Resolve term filter ("All" = no filter)
            AutoTable.Models.SimpleLookup? term = null;
            bool showAllTerms = string.Equals(SelectedTerm, "All", StringComparison.OrdinalIgnoreCase);
            try
            {
                var termLookups = await _dataService.GetTermLookupsAsync();
                if (!showAllTerms)
                    term = termLookups.FirstOrDefault(t => string.Equals(t.Name, SelectedTerm, StringComparison.OrdinalIgnoreCase));
            }
            catch (System.Exception ex) { errors.Add("terms: " + ex.Message); }

            // Term fees (all, so we can look up per-student expected amounts)
            var allTermFees = new System.Collections.Generic.List<AutoTable.Models.TermFee>();
            try
            {
                allTermFees = (await _dataService.GetTermFeesAsync()).ToList();
            }
            catch (System.Exception ex) { errors.Add("term fees: " + ex.Message); }

            // Payments scoped to selected filters
            System.Collections.Generic.IReadOnlyList<AutoTable.Models.FeePaymentSummary> payments = Array.Empty<AutoTable.Models.FeePaymentSummary>();
            try
            {
                payments = await _dataService.GetFeePaymentsAsync(cls?.Id, term?.Id);
            }
            catch (System.Exception ex) { errors.Add("payments: " + ex.Message); }

            // Students — the critical query; must not be blocked by other failures.
            System.Collections.Generic.IReadOnlyList<AutoTable.Models.Student> students = Array.Empty<AutoTable.Models.Student>();
            try
            {
                students = await _dataService.GetStudentsAsync();
            }
            catch (System.Exception ex) { errors.Add("students: " + ex.Message); }

            var roster = students
                .Where(s => s.IsActive && (cls == null || s.ClassId == cls.Id))
                .OrderBy(s => s.FullName)
                .ToList();

            int i = 1;
            foreach (var s in roster)
            {
                // Per-student expected amount: look up the TermFee for THIS student's class + selected term
                double studentExpected = 0;
                if (s.ClassId != null)
                {
                    if (term != null)
                        studentExpected = allTermFees.FirstOrDefault(tf => tf.ClassId == s.ClassId && tf.TermId == term.Id)?.Amount ?? 0;
                    else if (showAllTerms)
                    {
                        // When showing all terms, sum all term fees for this student's class
                        studentExpected = allTermFees.Where(tf => tf.ClassId == s.ClassId).Sum(tf => tf.Amount);
                    }
                }

                var studentPayments = payments.Where(p => p.StudentId == s.Id).ToList();
                var paid = studentPayments.Sum(p => p.Amount);

                // Apply available credits (overpayment carry-forward from previous terms)
                double creditApplied = 0;
                if (term != null)
                {
                    try
                    {
                        creditApplied = await _dataService.GetAvailableCreditAsync(s.Id, term.Id);
                    }
                    catch { /* credits table may not exist yet */ }
                }

                FeeRecords.Add(new FeeRecord
                {
                    RowNumber = i++,
                    StudentName = s.FullName,
                    AdmissionNumber = s.LIN ?? string.Empty,
                    ClassName = s.ClassName ?? (cls?.Name ?? "-"),
                    ExpectedAmount = (decimal)studentExpected,
                    PaidAmount = (decimal)(paid + creditApplied),
                    Term = showAllTerms ? "All Terms" : (term?.Name ?? "-"),
                    PaymentDate = studentPayments.Count > 0
                        ? studentPayments.Max(p => p.PaymentDate).ToString("dd MMM yyyy")
                        : "-"
                });
            }

            StatusMessage = errors.Count > 0
                ? $"Loaded {roster.Count} student(s). Errors: {string.Join("; ", errors)}"
                : $"Loaded {roster.Count} student(s); {payments.Count} payment(s) on record.";

            OnPropertyChanged(nameof(TotalExpected));
            OnPropertyChanged(nameof(TotalCollected));
            OnPropertyChanged(nameof(TotalOutstanding));
            OnPropertyChanged(nameof(CollectionRate));
        }
    }
}