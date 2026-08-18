using AutoTable.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class BudgetViewModel : BaseViewModel
    {
        [ObservableProperty] private string _selectedYear = "2025";

        public ObservableCollection<string> FinancialYears { get; } = new() { "2025", "2024", "2023" };
        public ObservableCollection<BudgetLine> BudgetLines { get; } = new();

        public decimal TotalBudget => BudgetLines.Sum(b => b.Budgeted);
        public decimal TotalSpent => BudgetLines.Sum(b => b.Spent);
        public decimal Remaining => TotalBudget - TotalSpent;
        public double UtilisationPercent => TotalBudget == 0 ? 0 : (double)(TotalSpent / TotalBudget * 100);

        public BudgetViewModel()
        {
            _ = LoadAsync();
        }

        partial void OnSelectedYearChanged(string value) => Load();

        [RelayCommand] private void Export() { }
        [RelayCommand] private void AddLineItem() { }

        private Task LoadAsync()
        {
            Load();
            return Task.CompletedTask;
        }

        private void Load()
        {
            BudgetLines.Clear();
            BudgetLines.Add(new BudgetLine { Category = "Teaching Staff Salaries",    Budgeted = 120_000_000, Spent = 74_400_000 });
            BudgetLines.Add(new BudgetLine { Category = "Support Staff Salaries",     Budgeted = 24_000_000,  Spent = 14_800_000 });
            BudgetLines.Add(new BudgetLine { Category = "School Supplies & Materials",Budgeted = 8_000_000,   Spent = 5_200_000  });
            BudgetLines.Add(new BudgetLine { Category = "Utilities (Water, Power)",   Budgeted = 6_000_000,   Spent = 4_100_000  });
            BudgetLines.Add(new BudgetLine { Category = "Infrastructure Maintenance", Budgeted = 15_000_000,  Spent = 3_200_000  });
            BudgetLines.Add(new BudgetLine { Category = "IT & Technology",            Budgeted = 5_000_000,   Spent = 4_800_000  });
            BudgetLines.Add(new BudgetLine { Category = "Sports & Co-curricular",     Budgeted = 4_000_000,   Spent = 1_600_000  });
            BudgetLines.Add(new BudgetLine { Category = "Examination Fees",           Budgeted = 3_500_000,   Spent = 3_500_000  });
            OnPropertyChanged(nameof(TotalBudget));
            OnPropertyChanged(nameof(TotalSpent));
            OnPropertyChanged(nameof(Remaining));
            OnPropertyChanged(nameof(UtilisationPercent));
        }
    }
}