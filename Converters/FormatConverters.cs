using Microsoft.UI.Xaml;
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

    public static class ThemeResourceHelper
    {
        /// <summary>
        /// Resolves a brush from the current theme's ThemeDictionary so converters
        /// return theme-aware colors at runtime.
        /// </summary>
        public static SolidColorBrush GetThemeBrush(string resourceKey)
        {
            try
            {
                if (Application.Current is Application app &&
                    app.Resources.ThemeDictionaries.TryGetValue(GetCurrentThemeKey(), out var dictObj) &&
                    dictObj is ResourceDictionary themeDict &&
                    themeDict.TryGetValue(resourceKey, out var brushObj) &&
                    brushObj is SolidColorBrush brush)
                {
                    return brush;
                }
            }
            catch { }

            // Fallback neutral gray
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 158, 158, 158));
        }

        private static string GetCurrentThemeKey()
        {
            return Application.Current?.RequestedTheme == ApplicationTheme.Dark ? "Dark" : "Light";
        }
    }

    public class GradeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var grade = value?.ToString() ?? string.Empty;
            return grade switch
            {
                "A" => ThemeResourceHelper.GetThemeBrush("SuccessGreenBrush"),   // excellent
                "B" => ThemeResourceHelper.GetThemeBrush("PrimaryBlueBrush"),    // good
                "C" => ThemeResourceHelper.GetThemeBrush("WarningOrangeBrush"),  // average
                "D" => ThemeResourceHelper.GetThemeBrush("WarningOrangeBrush"),  // below average
                "E" => ThemeResourceHelper.GetThemeBrush("DangerRedBrush"),      // poor
                "F" => ThemeResourceHelper.GetThemeBrush("DangerRedBrush"),      // fail
                _ => ThemeResourceHelper.GetThemeBrush("TextMutedBrush")         // muted
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    /// <summary>Rounds a numeric value to a whole number (no decimals, no % sign) for compact display in the donut.</summary>
    public class RoundPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                double d => ((int)Math.Round(d)).ToString(),
                float f => ((int)Math.Round(f)).ToString(),
                int i => i.ToString(),
                _ => value?.ToString() ?? string.Empty
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
                "Excellent" => ThemeResourceHelper.GetThemeBrush("SuccessGreenBrush"),   // good
                "On Track"  => ThemeResourceHelper.GetThemeBrush("PrimaryBlueBrush"),    // informational
                "At Risk"   => ThemeResourceHelper.GetThemeBrush("DangerRedBrush"),      // error
                "Present"   => ThemeResourceHelper.GetThemeBrush("SuccessGreenBrush"),   // present
                "Late"      => ThemeResourceHelper.GetThemeBrush("WarningOrangeBrush"),  // late
                "Absent"    => ThemeResourceHelper.GetThemeBrush("DangerRedBrush"),      // absent
                "Pending"   => ThemeResourceHelper.GetThemeBrush("WarningOrangeBrush"),  // pending
                "Approved"  => ThemeResourceHelper.GetThemeBrush("SuccessGreenBrush"),   // approved
                "Rejected"  => ThemeResourceHelper.GetThemeBrush("DangerRedBrush"),      // rejected
                "Active"        => ThemeResourceHelper.GetThemeBrush("SuccessGreenBrush"),   // active → green dot
                "Inactive"      => ThemeResourceHelper.GetThemeBrush("DangerRedBrush"),      // inactive → red dot (fallback)
                // StatusDotSource values (from Student model)
                "Completed"     => ThemeResourceHelper.GetThemeBrush("InfoBlueBrush"),      // completed course → blue dot
                "ChangedSchool" => ThemeResourceHelper.GetThemeBrush("WarningOrangeBrush"), // changed school → orange dot
                "Expelled"      => ThemeResourceHelper.GetThemeBrush("DangerRedBrush"),     // expelled → red dot
                "Other"         => ThemeResourceHelper.GetThemeBrush("DangerRedBrush"),     // other termination → red dot
                _ => ThemeResourceHelper.GetThemeBrush("TextMutedBrush")                 // muted
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts a Student.StatusDotSource string to a human-readable status label.
    /// Used by the status badge to display "Active", "Completed", "Expelled", etc.
    /// </summary>
    public class StatusLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value?.ToString() switch
            {
                "Active"        => "Active",
                "Completed"     => "Completed",
                "ChangedSchool" => "Changed School",
                "Expelled"      => "Expelled",
                _                => "Terminated"
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}