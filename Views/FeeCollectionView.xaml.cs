using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class FeeCollectionView : Page
    {
        public FeeCollectionViewModel ViewModel { get; } = new();
        public FeeCollectionView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
