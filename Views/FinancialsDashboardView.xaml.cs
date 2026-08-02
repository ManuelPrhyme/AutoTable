using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class FinancialsDashboardView : Page
    {
        public FinancialsDashboardViewModel ViewModel { get; } = new();
        public FinancialsDashboardView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
