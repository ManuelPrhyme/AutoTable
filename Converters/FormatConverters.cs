using Microsoft.UI.Xaml.Data;
using System;

namespace AutoTable.Converters
{
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                decimal d => $"UGX {d:N0}",
                double db => $"UGX {db:N0}",
                int i => $"UGX {i:N0}",
                _ => value?.ToString() ?? string.Empty
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class PercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                double d => $"{d:F1}%",
                float f => $"{f:F1}%",
                int i => $"{i}%",
                _ => value?.ToString() ?? string.Empty
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class DecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                double d => $"{d:F1}",
                float f => $"{f:F1}",
                decimal dec => $"{dec:F1}",
                _ => value?.ToString() ?? string.Empty
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
