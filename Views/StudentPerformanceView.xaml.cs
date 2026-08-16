using AutoTable.Models;
using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices.WindowsRuntime;


namespace AutoTable.Views
{
    public sealed partial class StudentPerformanceView : Page
    {
        public StudentPerformanceViewModel ViewModel { get; } = new();

        public StudentPerformanceView()
        {
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.RequestShowModal += OnRequestShowModal;
        }

        private async void OnRequestShowModal(object? sender, StudentPerformanceModalViewModel modalVm)
        {
            var modalView = new StudentPerformanceModalView(modalVm);

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["ModalDialogStyle"] as Style,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false,
            };

            // Size the dialog to 90% of the host page viewport
            try
            {
                var w = this.ActualWidth;
                var h = this.ActualHeight;
                if (!double.IsNaN(w) && !double.IsNaN(h) && w > 0 && h > 0)
                {
                    dialog.Width = w * 0.90;
                    dialog.Height = h * 0.90;
                }
            }
            catch { }

            // Ensure modal view fills available space
            modalView.HorizontalAlignment = HorizontalAlignment.Stretch;
            modalView.VerticalAlignment = VerticalAlignment.Stretch;

            dialog.Content = modalView;

            await System.WindowsRuntimeSystemExtensions.AsTask(dialog.ShowAsync());
        }

        private void OpenStudentModal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GradebookRow student)
            {
                ViewModel.OpenStudentModalCommand.Execute(student);
            }
        }
    }
}
