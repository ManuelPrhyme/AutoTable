using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
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

    public class GradeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var grade = value?.ToString() ?? string.Empty;
            return grade switch
            {
                "A" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),   // Green (#4CAF50) — excellent
                "B" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 33, 150, 243)),  // Blue (#2196F3) — good
                "C" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 152, 0)),   // Orange (#FF9800) — average
                "D" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 152, 0)),   // Orange (#FF9800) — below average
                "E" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54)),   // Red (#F44336) — poor
                "F" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54)),   // Red (#F44336) — fail
                _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 158, 158, 158))    // Gray (#9E9E9E) — muted
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var status = value?.ToString() ?? string.Empty;
            return status switch
            {
                "Excellent" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),   // Green (#4CAF50) — good
                "On Track"  => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 33, 150, 243)),  // Blue (#2196F3) — informational
                "At Risk"   => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54)),   // Red (#F44336) — error
                "Present"   => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),   // Green (#4CAF50) — present
                "Late"      => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 152, 0)),   // Orange (#FF9800) — late
                "Absent"    => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54)),   // Red (#F44336) — absent
                "Pending"   => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 152, 0)),   // Orange (#FF9800) — pending
                "Approved"  => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),   // Green (#4CAF50) — approved
                "Rejected"  => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54)),   // Red (#F44336) — rejected
                _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 158, 158, 158))            // Gray (#9E9E9E) — muted
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
