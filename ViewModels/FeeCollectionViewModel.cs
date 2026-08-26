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

        [ObservableProperty] private string _selectedClass = "P5";
        [ObservableProperty] private string _selectedTerm = "Term 2, 2025";
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

        partial void OnSelectedClassChanged(string value) => _ = Load();
        partial void OnSelectedTermChanged(string value) => _ = Load();

        [RelayCommand]
        private async Task Refresh() => await Load();

        // NOTE: payment recording lives in FeeCollectionView.RecordPayment_Click (code-behind)
        // which opens the searchable Record Payment modal and calls IDataService.CreateFeePaymentAsync.

        private async Task InitializeAsync()
        {
            var classes = await _dataService.GetClassesAsync();
            foreach (var c in classes) Classes.Add(c.Name);

            var terms = await _dataService.GetTermsAsync();
            foreach (var t in terms) Terms.Add(t);

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

            // Resolve class and term independently — a failure in one must not block the other.
            AutoTable.Models.SimpleLookup? cls = null;
            try
            {
                var classes = await _dataService.GetClassesAsync();
                cls = classes.FirstOrDefault(c => string.Equals(c.Name, SelectedClass, StringComparison.OrdinalIgnoreCase));
            }
            catch (System.Exception ex) { errors.Add("classes: " + ex.Message); }

            AutoTable.Models.SimpleLookup? term = null;
            try
            {
                var termLookups = await _dataService.GetTermLookupsAsync();
                term = termLookups.FirstOrDefault(t => string.Equals(t.Name, SelectedTerm, StringComparison.OrdinalIgnoreCase));
            }
            catch (System.Exception ex) { errors.Add("terms: " + ex.Message); }

            // Expected fee
            double expected = 0;
            try
            {
                var termFees = await _dataService.GetTermFeesAsync();
                expected = (cls != null && term != null)
                    ? termFees.FirstOrDefault(tf => tf.ClassId == cls.Id && tf.TermId == term.Id)?.Amount ?? 0
                    : 0;
            }
            catch (System.Exception ex) { errors.Add("term fees: " + ex.Message); }

            // Payments
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
                var studentPayments = payments.Where(p => p.StudentId == s.Id).ToList();
                var paid = studentPayments.Sum(p => p.Amount);

                FeeRecords.Add(new FeeRecord
                {
                    RowNumber = i++,
                    StudentName = s.FullName,
                    AdmissionNumber = s.LIN ?? string.Empty,
                    ClassName = s.ClassName ?? SelectedClass,
                    ExpectedAmount = (decimal)expected,
                    PaidAmount = (decimal)paid,
                    Term = SelectedTerm,
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