using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class SchoolSettingsView : Page
    {
        public SchoolSettingsViewModel ViewModel { get; } = new();

        public SchoolSettingsView()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveCommand.Execute(null);
        }
    }
}
