using AutoTable.Models;
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
            this.InitializeComponent();
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
            await _vm.AddStudentAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.Filter = SearchBox.Text;
        }
    }
}
