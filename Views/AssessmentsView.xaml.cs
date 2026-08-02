using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class AssessmentsView : Page
    {
        public AssessmentsViewModel ViewModel { get; } = new();
        public AssessmentsView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
