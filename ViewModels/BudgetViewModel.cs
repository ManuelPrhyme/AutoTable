using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class BudgetViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _selectedYear = "2025";
        [ObservableProperty] private string _statusMessage = string.Empty;

        public ObservableCollection<string> FinancialYears { get; } = new() { "2025", "2024", "2023" };
        public ObservableCollection<BudgetLine> BudgetLines { get; } = new();

        public decimal TotalBudget => BudgetLines.Sum(b => b.Budgeted);
        public decimal TotalSpent => BudgetLines.Sum(b => b.Spent);
        public decimal Remaining => TotalBudget - TotalSpent;
        public double UtilisationPercent => TotalBudget == 0 ? 0 : (double)(TotalSpent / TotalBudget * 100);

        public BudgetViewModel()
        {
            _dataService = AppServices.DataService ?? throw new InvalidOperationException("DataService not configured.");
            _ = LoadAsync();
        }

        partial void OnSelectedYearChanged(string value) => _ = LoadAsync();

        [RelayCommand]
        private async Task ExportAsync()
        {
            if (BudgetLines.Count == 0)
            {
                StatusMessage = "No budget lines to export.";
                return;
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Category,Budgeted,Spent,Remaining,Utilisation %,Status");
                foreach (var b in BudgetLines)
                {
                    sb.AppendLine($"\"{b.Category}\",{b.Budgeted},{b.Spent},{b.Remaining},{b.UtilisationPercent:F1},{b.Status}");
                }

                var fileName = $"budget_{SelectedYear}_{DateTime.UtcNow:yyyyMMdd}.csv";
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
                await File.WriteAllTextAsync(path, sb.ToString());
                StatusMessage = $"Exported to {path}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddLineItemAsync()
        {
            // ── ROLE-BASED GATING (dormant during development) ──────
            // Uncomment when enforcing admin-only budget editing:
            // if (!SessionService.Instance.IsAdministrator)
            // {
            //     StatusMessage = "Only administrators can add budget line items.";
            //     return;
            // }

            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Add Budget Line Item",
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                Content = new AddBudgetLineDialogContent()
            };

            var result = await dialog.ShowAsync();
            if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary) return;

            var content = (AddBudgetLineDialogContent)dialog.Content!;
            if (string.IsNullOrWhiteSpace(content.CategoryName))
            {
                StatusMessage = "Category name is required.";
                return;
            }

            try
            {
                var line = await _dataService.CreateBudgetLineAsync(new BudgetLine
                {
                    Category = content.CategoryName.Trim(),
                    Budgeted = content.BudgetedAmount,
                    Spent = 0,
                    FinancialYear = SelectedYear
                });
                BudgetLines.Insert(0, line);
                RefreshTotals();
                StatusMessage = $"Added budget line: {line.Category}";
            }
            catch (InvalidOperationException ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private async Task LoadAsync()
        {
            try
            {
                var lines = await _dataService.GetBudgetLinesAsync(SelectedYear);
                BudgetLines.Clear();
                foreach (var line in lines)
                    BudgetLines.Add(line);
                RefreshTotals();
                StatusMessage = BudgetLines.Count == 0
                    ? $"No budget lines for {SelectedYear}. Click 'Add Line Item' to create one."
                    : $"Loaded {BudgetLines.Count} budget line(s) for {SelectedYear}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading budget: {ex.Message}";
            }
        }

        private void RefreshTotals()
        {
            OnPropertyChanged(nameof(TotalBudget));
            OnPropertyChanged(nameof(TotalSpent));
            OnPropertyChanged(nameof(Remaining));
            OnPropertyChanged(nameof(UtilisationPercent));
        }
    }

    /// <summary>
    /// Simple content dialog content for adding a budget line item.
    /// </summary>
    public class AddBudgetLineDialogContent : Microsoft.UI.Xaml.Controls.StackPanel
    {
        private readonly Microsoft.UI.Xaml.Controls.TextBox _categoryBox;
        private readonly Microsoft.UI.Xaml.Controls.TextBox _amountBox;

        public string CategoryName => _categoryBox.Text;
        public decimal BudgetedAmount => decimal.TryParse(_amountBox.Text, out var v) ? v : 0;

        public AddBudgetLineDialogContent()
        {
            Spacing = 12;
            Width = 350;

            var catLabel = new Microsoft.UI.Xaml.Controls.TextBlock { Text = "Category Name", Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 4) };
            _categoryBox = new Microsoft.UI.Xaml.Controls.TextBox { PlaceholderText = "e.g. Teaching Staff Salaries" };

            var amtLabel = new Microsoft.UI.Xaml.Controls.TextBlock { Text = "Budgeted Amount", Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 4) };
            _amountBox = new Microsoft.UI.Xaml.Controls.TextBox { PlaceholderText = "e.g. 120000000" };

            Children.Add(catLabel);
            Children.Add(_categoryBox);
            Children.Add(amtLabel);
            Children.Add(_amountBox);
        }
    }
}