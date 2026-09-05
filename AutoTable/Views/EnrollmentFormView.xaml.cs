using AutoTable.Models;
using AutoTable.ViewModels;
using AutoTable.Views.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml.Media.Imaging;

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
            Loaded += EnrollmentFormView_Loaded;
        }

        private async void EnrollmentFormView_Loaded(object sender, RoutedEventArgs e)
        {
            try { await ViewModel.LoadLookupsAsync(); } catch { }
        }

        private async void PickPhoto_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            if (file == null) return;
            try
            {
                var bytes = await File.ReadAllBytesAsync(file.Path);
                ViewModel.PhotoBytes = bytes;
                var bitmap = new BitmapImage();
                using var ms = new MemoryStream(bytes);
                bitmap.SetSource(ms.AsRandomAccessStream());
                PhotoPreview.Source = bitmap;
            }
            catch { }
        }

        private void ClearPhoto_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PhotoBytes = null;
            PhotoPreview.Source = null;
        }

        /// <summary>
        /// Builds and shows the report-card preview for a newly enrolled student.
        /// Called by StudentsView right after the enrollment dialog is submitted.
        /// </summary>
        public async Task ShowPreviewForStudentAsync(Student student)
        {
            try
            {
                var service = AutoTable.AppServices.DataService;
                ReportCardSheetModel? model = null;
                if (service != null)
                {
                    try
                    {
                        model = await service.GetReportCardSheetAsync(
                            student.FullName,
                            student.ClassName ?? string.Empty,
                            string.Empty);
                    }
                    catch { model = null; }
                }

                if (model == null)
                {
                    model = new ReportCardSheetModel
                    {
                        StudentName = student.FullName,
                        AdmissionNumber = !string.IsNullOrWhiteSpace(student.LIN)
                            ? student.LIN
                            : student.AdmissionNumber ?? string.Empty,
                        ClassName = student.ClassName ?? string.Empty,
                        Stream = student.StreamName ?? string.Empty,
                        Gender = student.Gender ?? string.Empty,
                        DateOfBirth = student.DateOfBirth?.ToString("dd MMM yyyy") ?? string.Empty,
                        GuardianName = student.GuardianName ?? string.Empty,
                        GuardianPhone = student.GuardianPhone ?? string.Empty,
                        StudentPhotoBytes = student.PhotoBytes
                    };
                }

                var sheet = new ReportCardSheetView { DataContext = model };
                var dialog = new ContentDialog
                {
                    Title = $"Report Card - {student.FullName}",
                    Content = new ScrollViewer
                    {
                        Content = sheet,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    },
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot,
                    Width = 880,
                    Height = 720
                };
                await dialog.ShowAsync();
            }
            catch
            {
                // Preview is best-effort; never crash enrollment on it.
            }
        }
    }
}