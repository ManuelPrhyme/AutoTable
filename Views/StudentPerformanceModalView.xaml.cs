using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class StudentPerformanceModalView : UserControl
    {
        public StudentPerformanceModalViewModel ViewModel { get; }

        public StudentPerformanceModalView(StudentPerformanceModalViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
        }
    }
}
