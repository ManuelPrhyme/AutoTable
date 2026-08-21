using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class EnrollmentView : Page
    {
        private readonly EnrollmentViewModel _vm;

        public EnrollmentView()
        {
            InitializeComponent();
            _vm = new EnrollmentViewModel();
            DataContext = _vm;
        }
    }
}