using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class EnrollmentFormView : UserControl
    {
        public EnrollmentViewModel ViewModel { get; }

        public EnrollmentFormView()
        {
            InitializeComponent();
            ViewModel = new EnrollmentViewModel();
            DataContext = ViewModel;
        }
    }
}