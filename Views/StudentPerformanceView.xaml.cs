using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class StudentPerformanceView : Page
    {
        public StudentPerformanceViewModel ViewModel { get; } = new();
        public StudentPerformanceView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
