using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class MarksEntryView : Page
    {
        public MarksEntryViewModel ViewModel { get; } = new();

        public MarksEntryView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
