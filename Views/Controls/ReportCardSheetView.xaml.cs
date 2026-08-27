using AutoTable.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace AutoTable.Views.Controls
{
    public sealed partial class ReportCardSheetView : UserControl
    {
        public ReportCardSheetView()
        {
            this.InitializeComponent();
            this.Loaded += ReportCardSheetView_Loaded;
        }

        private void ReportCardSheetView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ReportCardSheetModel model && model.LogoBytes != null && model.LogoBytes.Length > 0)
            {
                var bitmap = new BitmapImage();
                using var ms = new MemoryStream(model.LogoBytes);
                bitmap.SetSource(ms.AsRandomAccessStream());
                SchoolLogo.Source = bitmap;
                SchoolLogo.Visibility = Visibility.Visible;
            }
            else
            {
                SchoolLogo.Visibility = Visibility.Collapsed;
            }
        }
    }
}
