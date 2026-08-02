using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class ReportCardsView : Page
    {
        public ReportCardsViewModel ViewModel { get; } = new();
        public ReportCardsView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
