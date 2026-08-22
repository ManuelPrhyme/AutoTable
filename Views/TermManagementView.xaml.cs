using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class TermManagementView : Page
    {
        private readonly TermManagementViewModel _vm;
        public TermManagementView()
        {
            InitializeComponent();
            _vm = new TermManagementViewModel();
            DataContext = _vm;
            Loaded += TermManagementView_Loaded;
        }

        private async void TermManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
        }

        private async void CreateTerm_Click(object sender, RoutedEventArgs e)
        {
            var name = NewTermName.Text?.Trim();
            var start = TermStart.Date;
            var end = TermEnd.Date;
            if (string.IsNullOrWhiteSpace(name)) return;
            await _vm.CreateTermAsync(name, start.DateTime, end.DateTime);
        }

        private async void SetFee_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is AutoTable.Models.SimpleLookup cls)
            {
                // find FeeBox in visual tree - simple approach: look up ancestor Grid
                var grid = ((FrameworkElement)sender).Parent as FrameworkElement;
                // fallback: ask user to select a term first
                if (TermsList.SelectedItem is not AutoTable.Models.SimpleLookup term) return;

                // find FeeBox by name within the template
                var container = ClassesFeeList.ContainerFromItem(cls) as ListViewItem;
                if (container == null) return;
                var root = container.ContentTemplateRoot as FrameworkElement;
                var feeBox = root?.FindName("FeeBox") as TextBox;
                if (feeBox == null) return;
                if (double.TryParse(feeBox.Text, out var amt))
                {
                    await _vm.SetTermFeeAsync(term.Id, cls.Id, amt);
                }
            }
        }
    }
}
