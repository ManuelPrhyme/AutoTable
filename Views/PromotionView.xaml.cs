using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class PromotionView : Page
    {
        private readonly PromotionViewModel _vm;

        public PromotionView()
        {
            InitializeComponent();
            _vm = new PromotionViewModel();
            DataContext = _vm;
        }
    }
}
