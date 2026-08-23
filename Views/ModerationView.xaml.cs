using AutoTable.Models;
using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class ModerationView : Page
    {
        public ModerationViewModel ViewModel { get; } = new();
        public ModerationView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private async void ApproveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ModerationItem item)
                await ViewModel.ApproveItemAsync(item);
        }

        private async void RejectItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ModerationItem item)
                await ViewModel.RejectItemAsync(item);
        }
    }
}