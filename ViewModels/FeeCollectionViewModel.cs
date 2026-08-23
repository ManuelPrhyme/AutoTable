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
                try
                {
                    var lin = linBox.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(lin)) return;
                    if (!double.TryParse(amountBox.Text, out var amt)) return;

                    // find student by LIN
                    var students = await _dataService.GetStudentsAsync();
                    var student = students.FirstOrDefault(s => string.Equals(s.LIN, lin, StringComparison.OrdinalIgnoreCase) || string.Equals(s.AdmissionNumber, lin, StringComparison.OrdinalIgnoreCase));
                    if (student == null)
                    {
                        StatusMessage = $"No student found with LIN '{lin}'.";
                        return;
                    }

                    // resolve the selected term id so the payment is attributed correctly
                    int? termId = null;
                    var termLookups = await _dataService.GetTermLookupsAsync();
                    var selectedTerm = termLookups.FirstOrDefault(t => string.Equals(t.Name, SelectedTerm, StringComparison.OrdinalIgnoreCase));
                    if (selectedTerm != null) termId = selectedTerm.Id;

                    await _dataService.CreateFeePaymentAsync(student.Id, amt, termId, null, "Recorded via UI");
                    StatusMessage = $"Recorded payment of {amt:N0} for {student.FullName}.";
                    await Load();
                }
                catch (System.Exception ex)
                {
                    StatusMessage = "Failed to record payment: " + ex.Message;
                }
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

        /// <summary>
        /// Builds fee records from real data: expected amount from the per-class term-fee
        /// configuration and paid amounts from actual FeePayments rows in the database.
        /// </summary>
        private async Task Load()
        {
            FeeRecords.Clear();

            try
            {
                // Resolve the selected class / term ids
                var classes = await _dataService.GetClassesAsync();
                var termLookups = await _dataService.GetTermLookupsAsync();
                var cls = classes.FirstOrDefault(c => string.Equals(c.Name, SelectedClass, StringComparison.OrdinalIgnoreCase));
                var term = termLookups.FirstOrDefault(t => string.Equals(t.Name, SelectedTerm, StringComparison.OrdinalIgnoreCase));

                // Expected amount for this class+term from the fee configuration
                var termFees = await _dataService.GetTermFeesAsync();
                double expected = (cls != null && term != null)
                    ? termFees.FirstOrDefault(tf => tf.ClassId == cls.Id && tf.TermId == term.Id)?.Amount ?? 0
                    : 0;

                // Actual payments recorded for this class (+term when known)
                var payments = await _dataService.GetFeePaymentsAsync(cls?.Id, term?.Id);

                // Roster of active students in the class
                var students = await _dataService.GetStudentsAsync();
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

                StatusMessage = $"Loaded {roster.Count} student(s); {payments.Count} payment(s) on record.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to load fee records: " + ex.Message;
            }

            OnPropertyChanged(nameof(TotalExpected));
            OnPropertyChanged(nameof(TotalCollected));
            OnPropertyChanged(nameof(TotalOutstanding));
            OnPropertyChanged(nameof(CollectionRate));
        }
    }
}