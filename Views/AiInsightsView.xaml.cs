using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class AiInsightsView : Page
    {
        public AiInsightsViewModel ViewModel { get; } = new();
        public AiInsightsView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
