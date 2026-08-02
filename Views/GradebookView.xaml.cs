using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class GradebookView : Page
    {
        public GradebookViewModel ViewModel { get; } = new();

        public GradebookView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
