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

            // Size the dialog: 85% width of host page viewport, keep height at 90%
            try
            {
                var w = this.ActualWidth;
                var h = this.ActualHeight;
                if (!double.IsNaN(w) && !double.IsNaN(h) && w > 0 && h > 0)
                {
                    dialog.Width = w * 0.85;
                    dialog.Height = h * 0.90;
                }
            }
            catch { }

            // Ensure modal view fills available space
            modalView.HorizontalAlignment = HorizontalAlignment.Stretch;
            modalView.VerticalAlignment = VerticalAlignment.Stretch;

            // Wrap modal view in a container so we can overlay a close button in the top-right
            var container = new Grid();
            container.Children.Add(modalView);

            var closeButton = new Button
            {
                Content = "?",
                Width = 36,
                Height = 36,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(8),
                FontSize = 16,
                BorderThickness = new Thickness(0),
                Style = Application.Current.Resources["DialogCloseButtonStyle"] as Style,
            };

            // Add tooltip and click handler (async to avoid blocking UI)
            ToolTipService.SetToolTip(closeButton, "Close");
            closeButton.Click += async (_, _) =>
            {
                try { dialog.Hide(); } catch { }
                await System.Threading.Tasks.Task.CompletedTask;
            };

            // Ensure close button receives input
            closeButton.IsHitTestVisible = true;
            container.Children.Add(closeButton);

            dialog.Content = container;

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
