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
        // Direct color constants so the converter never depends on theme dictionary lookups.
        private static readonly SolidColorBrush GreenBrush  = new(Windows.UI.Color.FromArgb(255, 39, 174, 96));   // #27AE60
        private static readonly SolidColorBrush BlueBrush   = new(Windows.UI.Color.FromArgb(255, 41, 128, 185));  // #2980B9
        private static readonly SolidColorBrush RedBrush    = new(Windows.UI.Color.FromArgb(255, 231, 76, 60));   // #E74C3C
        private static readonly SolidColorBrush OrangeBrush = new(Windows.UI.Color.FromArgb(255, 243, 156, 18));  // #F39C12
        private static readonly SolidColorBrush MutedBrush  = new(Windows.UI.Color.FromArgb(255, 158, 158, 158)); // #9E9E9E

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var status = value?.ToString() ?? string.Empty;
            return status switch
            {
                // StatusDotSource values (from Student model)
                "Active"        => GreenBrush,   // active → green dot
                "Completed"     => BlueBrush,    // completed course → blue dot
                "ChangedSchool" => OrangeBrush,  // changed school → orange dot
                "Expelled"      => RedBrush,     // expelled → red dot
                "Other"         => RedBrush,     // other termination → red dot
                // Legacy / other statuses
                "Excellent"     => GreenBrush,
                "On Track"      => BlueBrush,
                "At Risk"       => RedBrush,
                "Present"       => GreenBrush,
                "Late"          => OrangeBrush,
                "Absent"        => RedBrush,
                "Pending"       => OrangeBrush,
                "Approved"      => GreenBrush,
                "Rejected"      => RedBrush,
                _                => MutedBrush,
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts a Student.StatusDotSource string to a human-readable status label.
    /// Active students show "Active"; all terminated students show "Inactive"
    /// with the specific cause displayed separately below.
    /// </summary>
    public class StatusLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value?.ToString() switch
            {
                "Active"        => "Active",
                _                => "Inactive"  // all terminated reasons show as Inactive
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Formats a balance value for display.
    /// Negative balance (surplus) → "+UGX {abs}" (blue)
    /// Zero balance (paid) → "UGX 0" (green)
    /// Positive balance (owed) → "UGX {value}" (red)
    /// </summary>
    public class BalanceTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            decimal amount = value switch
            {
                decimal d => d,
                double db => (decimal)db,
                int i => i,
                _ => 0
            };

            if (amount < 0)
                return $"+UGX {Math.Abs(amount):N0}";  // surplus
            return $"UGX {amount:N0}";
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the appropriate color for a balance value.
    /// Negative balance (surplus) → Blue
    /// Zero balance (paid in full) → Green
    /// Positive balance (still owes) → Red
    /// </summary>
    public class BalanceColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush BlueBrush = new(Windows.UI.Color.FromArgb(255, 41, 128, 185));   // surplus
        private static readonly SolidColorBrush GreenBrush = new(Windows.UI.Color.FromArgb(255, 39, 174, 96));   // paid
        private static readonly SolidColorBrush RedBrush = new(Windows.UI.Color.FromArgb(255, 231, 76, 60));     // owed

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            decimal amount = value switch
            {
                decimal d => d,
                double db => (decimal)db,
                int i => i,
                _ => 0
            };

            if (amount < 0) return BlueBrush;   // surplus (overpaid)
            if (amount == 0) return GreenBrush; // paid in full
            return RedBrush;                    // still owes
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}