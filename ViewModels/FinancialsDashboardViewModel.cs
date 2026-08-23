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
    public partial class FinancialsDashboardViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private decimal _totalCollected;
        [ObservableProperty] private decimal _outstandingFees;
        [ObservableProperty] private double _budgetUsedPercent;
        [ObservableProperty] private double _studentsPaidPercent;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public ObservableCollection<Transaction> RecentTransactions { get; } = new();

        public FinancialsDashboardViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            _ = LoadAsync();
        }

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();

        private async Task LoadAsync()
        {
            try
            {
                // Actual collections from the FeePayments table
                var payments = await _dataService.GetFeePaymentsAsync();
                TotalCollected = (decimal)payments.Sum(p => p.Amount);

                // Expected fees = configured per-class term fees × active students in each class
                var termFees = await _dataService.GetTermFeesAsync();
                var students = (await _dataService.GetStudentsAsync()).Where(s => s.IsActive).ToList();

                decimal expectedTotal = 0;
                foreach (var tf in termFees)
                {
                    var count = students.Count(s => s.ClassId == tf.ClassId);
                    expectedTotal += (decimal)(tf.Amount * count);
                }

                OutstandingFees = expectedTotal > TotalCollected ? expectedTotal - TotalCollected : 0;
                BudgetUsedPercent = expectedTotal == 0 ? 0 : Math.Round((double)(TotalCollected / expectedTotal * 100), 1);

                // % of active students who have made at least one payment
                var payingStudentIds = payments.Select(p => p.StudentId).Distinct().ToHashSet();
                var paidCount = students.Count(s => payingStudentIds.Contains(s.Id));
                StudentsPaidPercent = students.Count == 0 ? 0 : Math.Round(paidCount * 100.0 / students.Count, 1);

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

                StatusMessage = $"Loaded {payments.Count} payment(s) from the database.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to load financials: " + ex.Message;
            }
        }
    }
}