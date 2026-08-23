using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Microsoft.UI.Xaml;

namespace AutoTable.Views
{
    public sealed partial class FeeCollectionView : Page
    {
        public FeeCollectionViewModel ViewModel { get; } = new();
        public FeeCollectionView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private async void RecordPayment_Click(object sender, RoutedEventArgs e)
        {
            // Pre-fill expected amount from selected class & term if available
            var selectedClass = ViewModel.SelectedClass;
            var selectedTerm = ViewModel.SelectedTerm;

            double? expected = null;
            int? termId = null;
            try
            {
                var fees = await AppServices.DataService.GetTermFeesAsync();
                var match = fees.FirstOrDefault(tf => tf.ClassName == selectedClass && tf.TermName == selectedTerm);
                if (match != null)
                {
                    expected = match.Amount;
                    termId = match.TermId;
                }
            }
            catch { }

            var dialog = new ContentDialog
            {
                Title = "Record Fee Payment",
                PrimaryButtonText = "Record",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var stack = new StackPanel { Spacing = 8 };
            var linBox = new TextBox { Header = "Student LIN", PlaceholderText = "LIN or admission number" };
            var amountBox = new TextBox { Header = "Amount", PlaceholderText = "Amount to record", Text = expected.HasValue ? expected.Value.ToString() : string.Empty };
            stack.Children.Add(linBox);
            stack.Children.Add(amountBox);
            dialog.Content = stack;

            var res = await dialog.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                var lin = linBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(lin)) return;
                if (!double.TryParse(amountBox.Text, out var amt)) return;

                var students = await AppServices.DataService.GetStudentsAsync();
                var student = students.FirstOrDefault(s => string.Equals(s.LIN, lin, StringComparison.OrdinalIgnoreCase) || string.Equals(s.AdmissionNumber, lin, StringComparison.OrdinalIgnoreCase));
                if (student == null) return;

                await AppServices.DataService.CreateFeePaymentAsync(student.Id, amt, termId, null, "Recorded via UI");
                await ViewModel.RefreshCommand.ExecuteAsync(null);
            }
        }
    }
}
