using AutoTable.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AutoTable.ViewModels
{
    public partial class FinancialsDashboardViewModel : BaseViewModel
    {
        [ObservableProperty] private decimal _totalCollected = 48_500_000;
        [ObservableProperty] private decimal _outstandingFees = 12_300_000;
        [ObservableProperty] private double _budgetUsedPercent = 61.4;
        [ObservableProperty] private double _studentsPaidPercent = 79.8;

        public ObservableCollection<Transaction> RecentTransactions { get; } = new();

        public FinancialsDashboardViewModel()
        {
            RecentTransactions.Add(new Transaction { StudentName = "Amina Nakato",   ClassName = "P5", Term = "T2 2025", Amount = 450_000, PaymentDate = "02 Aug 2025", Status = "Confirmed" });
            RecentTransactions.Add(new Transaction { StudentName = "Brian Okello",   ClassName = "P4", Term = "T2 2025", Amount = 450_000, PaymentDate = "01 Aug 2025", Status = "Confirmed" });
            RecentTransactions.Add(new Transaction { StudentName = "Carol Namukasa", ClassName = "P6", Term = "T2 2025", Amount = 225_000, PaymentDate = "31 Jul 2025", Status = "Partial" });
            RecentTransactions.Add(new Transaction { StudentName = "David Ssempijja",ClassName = "P3", Term = "T2 2025", Amount = 450_000, PaymentDate = "30 Jul 2025", Status = "Confirmed" });
            RecentTransactions.Add(new Transaction { StudentName = "Esther Akello",  ClassName = "P7", Term = "T2 2025", Amount = 450_000, PaymentDate = "29 Jul 2025", Status = "Confirmed" });
        }
    }
}
