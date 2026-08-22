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



        [RelayCommand]
        private async Task RecordPaymentAsync()
        {
            // Simple dialog to record a payment: ask for student LIN and amount
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Record Fee Payment",
                PrimaryButtonText = "Record",
                CloseButtonText = "Cancel",
                XamlRoot = Microsoft.UI.Xaml.Window.Current.Content.XamlRoot
            };

            var stack = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing = 8 };
            var linBox = new Microsoft.UI.Xaml.Controls.TextBox { Header = "Student LIN", PlaceholderText = "LIN or admission number" };
            var amountBox = new Microsoft.UI.Xaml.Controls.TextBox { Header = "Amount", PlaceholderText = "Amount to record" };
            stack.Children.Add(linBox);
            stack.Children.Add(amountBox);
            dialog.Content = stack;

            var res = await dialog.ShowAsync();
            if (res == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                var lin = linBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(lin)) return;
                if (!double.TryParse(amountBox.Text, out var amt)) return;

                // find student by LIN
                var students = await _dataService.GetStudentsAsync();
                var student = students.FirstOrDefault(s => string.Equals(s.LIN, lin, StringComparison.OrdinalIgnoreCase) || string.Equals(s.AdmissionNumber, lin, StringComparison.OrdinalIgnoreCase));
                if (student == null) return;

                await _dataService.CreateFeePaymentAsync(student.Id, amt, null, "Recorded via UI");
                await Load();
            }
        }

        private async Task InitializeAsync()
        {
            var classes = await _dataService.GetClassesAsync();
            foreach (var c in classes) Classes.Add(c.Name);

            var terms = await _dataService.GetTermsAsync();
            foreach (var t in terms) Terms.Add(t);

            await Load();
        }

        private async Task Load()
        {
            FeeRecords.Clear();
            var students = await _dataService.GetStudentMarksAsync(SelectedClass, "Mathematics", "Mid Term I");
            int i = 1;
            var rng = new System.Random(SelectedClass.GetHashCode());
            foreach (var s in students)
            {
                decimal expected = 450_000;
                decimal paid = rng.Next(0, 3) switch { 0 => 0, 1 => 225_000, _ => 450_000 };
                FeeRecords.Add(new FeeRecord
                {
                    RowNumber = i++,
                    StudentName = s.StudentName,
                    AdmissionNumber = s.AdmissionNumber,
                    ClassName = s.ClassName,
                    ExpectedAmount = expected,
                    PaidAmount = paid,
                    Term = SelectedTerm,
                    PaymentDate = paid > 0 ? "Jul 2025" : "-"
                });
            }
            OnPropertyChanged(nameof(TotalExpected));
            OnPropertyChanged(nameof(TotalCollected));
            OnPropertyChanged(nameof(TotalOutstanding));
            OnPropertyChanged(nameof(CollectionRate));
        }
    }
}