using AutoTable.ViewModels;
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
    }
}
