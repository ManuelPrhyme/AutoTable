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
            form.ViewModel.OnSubmittedAsync = async () =>
            {
                await _vm.LoadAsync();
                if (dialog != null) StudentsList.ItemsSource = _vm.Students;
                dialog?.Hide();
            };

            dialog = new ContentDialog
            {
                Title = "Student Enrollment",
                Content = form,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Width = 600,
                Height = 720,
                IsPrimaryButtonEnabled = false
            };

            await dialog.ShowAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.Filter = SearchBox.Text;
        }
    }
}