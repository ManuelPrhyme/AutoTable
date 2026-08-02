using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class BudgetView : Page
    {
        public BudgetViewModel ViewModel { get; } = new();
        public BudgetView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
