using AutoTable.Services;
using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.Views
{
    public sealed partial class FinancialsDashboardView : Page
    {
        public FinancialsDashboardViewModel ViewModel { get; } = new();
        public FinancialsDashboardView()
        {
            InitializeComponent();
            DataContext = ViewModel;
            Loaded += FinancialsDashboardView_Loaded;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private async void FinancialsDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFeeCollectionByClassAsync();
        }

        // Re-render the per-class chart whenever the selected term changes so it stays
        // in sync with the KPI cards (which are already term-scoped).
        private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.SelectedTermName))
                await LoadFeeCollectionByClassAsync();
        }

        private async Task LoadFeeCollectionByClassAsync()
        {
            FeeCollectionByClassPanel.Children.Clear();
            try
            {
                var ds = AppServices.DataService;
                if (ds == null) return;

                var classes = await ds.GetClassesAsync();
                var students = (await ds.GetStudentsAsync()).Where(s => s.IsActive).ToList();

                // Resolve the term selected on the dashboard so the chart reflects the SAME
                // term as the KPI cards instead of an all-time / general metric.
                bool showAll = string.IsNullOrEmpty(ViewModel.SelectedTermName)
                    || string.Equals(ViewModel.SelectedTermName, "All Terms", StringComparison.OrdinalIgnoreCase);
                int? termId = null;
                if (!showAll)
                {
                    var termLookups = await ds.GetTermLookupsAsync();
                    termId = termLookups.FirstOrDefault(t => string.Equals(t.Name, ViewModel.SelectedTermName, StringComparison.OrdinalIgnoreCase))?.Id;
                }

                var termFees = await ds.GetTermFeesAsync();
                // Payments scoped to the selected term (null termId = all terms)
                var payments = await ds.GetFeePaymentsAsync(null, termId);
                // Per-class expected fees scoped to the selected term (all terms → sum of every term's fee)
                var classFees = termFees
                    .Where(tf => termId == null || tf.TermId == termId.Value)
                    .GroupBy(tf => tf.ClassId)
                    .ToDictionary(g => g.Key, g => g.Sum(tf => tf.Amount));

                if (classes.Count == 0)
                {
                    FeeCollectionByClassPanel.Children.Add(new TextBlock
                    {
                        Text = "No classes found.",
                        FontSize = 13,
                        Foreground = (Brush)Application.Current.Resources["TextMutedBrush"]
                    });
                    return;
                }

                // Update the term label with the selected term
                FeeCollectionTermLabel.Text = showAll ? "All Terms" : ViewModel.SelectedTermName;

                // Build per-class collected vs outstanding data
                var classData = new List<(string Name, double Collected, double Outstanding)>();
                foreach (var cls in classes)
                {
                    var classStudents = students.Where(s => s.ClassId == cls.Id).ToList();
                    var classTermFee = classFees.TryGetValue(cls.Id, out var fee) ? fee : 0;
                    var expected = classTermFee * classStudents.Count;

                    // Sum payments for students in this class (already term-scoped)
                    var studentIds = classStudents.Select(s => s.Id).ToHashSet();
                    var collected = payments.Where(p => studentIds.Contains(p.StudentId)).Sum(p => p.Amount);
                    var outstanding = expected > collected ? expected - collected : 0;

                    classData.Add((cls.Name, collected, outstanding));
                }

                // Find the max total (collected + outstanding) for scaling
                double maxTotal = classData.Max(c => c.Collected + c.Outstanding);
                if (maxTotal == 0) maxTotal = 1; // avoid division by zero
                const double maxBarHeight = 130; // max pixel height for the tallest bar

                var greenBrush = (Brush)Application.Current.Resources["SuccessGreenBrush"];
                var redBrush = (Brush)Application.Current.Resources["DangerRedBrush"];
                var textSecondary = (Brush)Application.Current.Resources["TextSecondaryBrush"];

                foreach (var (name, collected, outstanding) in classData)
                {
                    var total = collected + outstanding;
                    var collectedHeight = total > 0 ? (collected / maxTotal) * maxBarHeight : 0;
                    var outstandingHeight = total > 0 ? (outstanding / maxTotal) * maxBarHeight : 0;

                    var barGroup = new StackPanel { Spacing = 4 };
                    var bars = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Bottom };

                    bars.Children.Add(new Border
                    {
                        Width = 28,
                        Height = Math.Max(collectedHeight, 2),
                        Background = greenBrush,
                        CornerRadius = new CornerRadius(4),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Opacity = 0.85
                    });
                    bars.Children.Add(new Border
                    {
                        Width = 28,
                        Height = Math.Max(outstandingHeight, outstanding > 0 ? 2 : 0),
                        Background = redBrush,
                        CornerRadius = new CornerRadius(4),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Opacity = 0.7
                    });

                    barGroup.Children.Add(bars);
                    barGroup.Children.Add(new TextBlock
                    {
                        Text = name,
                        FontSize = 11,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = textSecondary,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });

                    FeeCollectionByClassPanel.Children.Add(barGroup);
                }
            }
            catch
            {
                FeeCollectionByClassPanel.Children.Add(new TextBlock
                {
                    Text = "Failed to load class data.",
                    FontSize = 13,
                    Foreground = (Brush)Application.Current.Resources["TextMutedBrush"]
                });
            }
        }
    }
}
