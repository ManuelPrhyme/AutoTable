using System;
using System.Threading.Tasks;
using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class StudentsView : Page
    {
        private readonly StudentsViewModel _vm;

        public StudentsView()
        {
            InitializeComponent();
            _vm = new StudentsViewModel();
            DataContext = _vm;
            Loaded += StudentsView_Loaded;
        }

        private async void StudentsView_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
            StudentsList.ItemsSource = _vm.Students;
        }

        private async void AddStudent_Click(object sender, RoutedEventArgs e)
        {
            var form = new EnrollmentFormView();

            ContentDialog? dialog = null;
            form.ViewModel.OnSubmittedAsync = async (createdStudent) =>
            {
                if (createdStudent != null)
                {
                    // Insert newly created student at top of collection so it appears first
                    _vm.Students.Insert(0, createdStudent);
                }
                else
                {
                    // Fallback: reload full list
                    await _vm.LoadAsync();
                    StudentsList.ItemsSource = _vm.Students;
                }
                dialog?.Hide();
            };

            // Modal-size.md standard: 1040 x 577 dialog. WinUI clamps ContentDialog width
            // via ContentDialogMaxWidth (~548px default) — the override is REQUIRED or the
            // two-column layout gets silently clipped.
            dialog = new ContentDialog
            {
                Title = "Enroll New Student",
                Content = form,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };
            dialog.Resources["ContentDialogMaxWidth"] = 1040d + 48d; // width + padding allowance

            await dialog.ShowAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.Filter = SearchBox.Text;
        }
    }
}