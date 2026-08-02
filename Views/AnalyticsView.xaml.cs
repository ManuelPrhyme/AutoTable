using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class AnalyticsView : Page
    {
        public AnalyticsViewModel ViewModel { get; } = new();
        public AnalyticsView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
