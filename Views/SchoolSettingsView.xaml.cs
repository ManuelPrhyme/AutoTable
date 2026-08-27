using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace AutoTable.Views
{
    public sealed partial class SchoolSettingsView : Page
    {
        public SchoolSettingsViewModel ViewModel { get; } = new();

        public SchoolSettingsView()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
            this.Loaded += SchoolSettingsView_Loaded;
        }

        private void SchoolSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshLogoPreview();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveCommand.Execute(null);
        }

        private async void PickLogo_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".gif");

            // WinUI 3: must set HWND for picker
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var bytes = await File.ReadAllBytesAsync(file.Path);
                ViewModel.LogoBytes = bytes;
                RefreshLogoPreview();
            }
        }

        private void ClearLogo_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LogoBytes = null;
            LogoPreview.Source = null;
        }

        private void RefreshLogoPreview()
        {
            if (ViewModel.LogoBytes != null && ViewModel.LogoBytes.Length > 0)
            {
                var bitmap = new BitmapImage();
                using var ms = new MemoryStream(ViewModel.LogoBytes);
                bitmap.SetSource(ms.AsRandomAccessStream());
                LogoPreview.Source = bitmap;
            }
            else
            {
                LogoPreview.Source = null;
            }
        }
    }
}
