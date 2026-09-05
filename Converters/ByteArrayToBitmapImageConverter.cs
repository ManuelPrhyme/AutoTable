using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using Windows.Storage.Streams;

namespace AutoTable.Converters
{
    public class ByteArrayToBitmapImageConverter : IValueConverter
    {
        // A tiny silhouette PNG (transparent background) encoded as base64 used as fallback.
        // A short placeholder is embedded so no external asset is required.
        private const string SilhouetteBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAABG0lEQVR4nO3UsQ3AIBAEQeP+f0d7i" +
            "QxqQe5O2i3Qz/7wqgQIECBAgAABAgQIECBAgAABAgQIECBAgAABAgQIECBAgAABAgQIECBAgAAB" +
            "AgQIECBAgAABAgQIECBAgAABAv4Gv0Bv2p3Wq3w0AAAAASUVORK5CYII="; // tiny placeholder

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                byte[]? bytes = value as byte[];
                if (bytes == null || bytes.Length == 0)
                    bytes = System.Convert.FromBase64String(SilhouetteBase64);

                var bitmap = new BitmapImage();
                using var ms = new MemoryStream(bytes);
                bitmap.SetSource(ms.AsRandomAccessStream());
                return bitmap;
            }
            catch
            {
                return null!;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
