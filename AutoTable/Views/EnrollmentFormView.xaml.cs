using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;

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
            ViewModel.ErrorOccurred += async (msg) =>
            {
                try
                {
                    var dlg = new ContentDialog
                    {
                        Title = "Enrollment error",
                        Content = msg,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dlg.ShowAsync();
                }
                catch { }
            };
        }
    }
}