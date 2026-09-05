using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class AuditLogView : Page
    {
        private readonly AuditLogViewModel _vm;

        public AuditLogView()
        {
            InitializeComponent();
            _vm = new AuditLogViewModel();
            DataContext = _vm;
            Loaded += AuditLogView_Loaded;
        }

        private async void AuditLogView_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
            LogsList.ItemsSource = _vm.FilteredLogs;
            EnrollmentsList.ItemsSource = _vm.FilteredEnrollments;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.Filter = SearchBox.Text;
        }
    }
}