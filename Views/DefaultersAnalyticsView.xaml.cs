using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace AutoTable.Views
{
    public sealed partial class DefaultersAnalyticsView : Page
    {
        private readonly DefaultersAnalyticsViewModel _vm;

        public DefaultersAnalyticsView()
        {
            InitializeComponent();
            _vm = new DefaultersAnalyticsViewModel();
            DataContext = _vm;
            _vm.CohortSummaries.CollectionChanged += (_, _) => { BuildCollectionRateChart(); BuildPieChart(); };
            _vm.Defaulters.CollectionChanged += (_, _) => BuildPieChart();
        }

        private void MinBalanceBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(MinBalanceBox.Text, out var val) && val >= 0)
                _vm.MinBalance = val;
            else if (string.IsNullOrWhiteSpace(MinBalanceBox.Text))
                _vm.MinBalance = 0;
        }

        private void BuildPieChart()
        {
            PieCanvas.Children.Clear();

            int paid = _vm.PaidInScope;
            int partial = _vm.PartialInScope;
            int unpaid = _vm.UnpaidInScope;
            int total = paid + partial + unpaid;

            PieCenterCount.Text = total.ToString();

            if (total == 0) return;

            double cx = 100, cy = 100, radius = 90;
            double startAngle = -90; // start from top (12 o'clock)

            var sliceData = new[]
            {
                new { Count = paid, Brush = (Brush)Resources["SuccessGreenBrush"] },
                new { Count = partial, Brush = (Brush)Resources["WarningOrangeBrush"] },
                new { Count = unpaid, Brush = (Brush)Resources["ErrorRedBrush"] }
            };

            foreach (var slice in sliceData)
            {
                if (slice.Count == 0) continue;
                double sweepAngle = (slice.Count / (double)total) * 360;

                var path = new Microsoft.UI.Xaml.Shapes.Path
                {
                    Fill = slice.Brush,
                    Stroke = (Brush)Resources["CardBackgroundBrush"],
                    StrokeThickness = 2
                };

                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(cx, cy), IsClosed = true };

                double startRad = startAngle * Math.PI / 180;
                double endRad = (startAngle + sweepAngle) * Math.PI / 180;

                var line = new LineSegment
                {
                    Point = new Point(cx + radius * Math.Cos(startRad), cy + radius * Math.Sin(startRad))
                };
                figure.Segments.Add(line);

                var arc = new ArcSegment
                {
                    Point = new Point(cx + radius * Math.Cos(endRad), cy + radius * Math.Sin(endRad)),
                    Size = new Size(radius, radius),
                    RotationAngle = sweepAngle,
                    IsLargeArc = sweepAngle > 180,
                    SweepDirection = SweepDirection.Clockwise
                };
                figure.Segments.Add(arc);

                geometry.Figures.Add(figure);
                path.Data = geometry;

                PieCanvas.Children.Add(path);
                startAngle += sweepAngle;
            }
        }

        private void BuildCollectionRateChart()
        {
            BarChartPanel.Children.Clear();
            // Skip the school-wide summary (index 0), show per-class bars
            var classData = _vm.CohortSummaries
                .Where(c => c.Label != "School-wide" && c.TotalStudents > 0)
                .ToList();
            if (classData.Count == 0) return;

            var maxRate = classData.Max(c => c.CollectionRate);
            if (maxRate <= 0) maxRate = 100;

            foreach (var cls in classData)
            {
                var barHeight = (cls.CollectionRate / maxRate) * 160;
                if (barHeight < 4 && cls.CollectionRate > 0) barHeight = 4;

                // Color: green >= 80%, amber >= 50%, red < 50%
                var brush = cls.CollectionRate >= 80
                    ? (Brush)Resources["SuccessGreenBrush"]
                    : cls.CollectionRate >= 50
                        ? (Brush)Resources["WarningAmberBrush"]
                        : (Brush)Resources["ErrorRedBrush"];

                var bar = new Border
                {
                    Width = 40,
                    Height = barHeight,
                    Background = brush,
                    CornerRadius = new CornerRadius(4),
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                var rateLabel = new TextBlock
                {
                    Text = $"{cls.CollectionRate:F0}%",
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = (Brush)Resources["TextSecondaryBrush"],
                    Margin = new Thickness(0, 2, 0, 0)
                };

                var nameLabel = new TextBlock
                {
                    Text = cls.Label,
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = (Brush)Resources["TextSecondaryBrush"]
                };

                var detailLabel = new TextBlock
                {
                    Text = $"{cls.PaidCount}/{cls.TotalStudents} paid",
                    FontSize = 9,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = (Brush)Resources["TextSecondaryBrush"]
                };

                var col = new StackPanel { Spacing = 4, Width = 60 };
                col.Children.Add(rateLabel);
                col.Children.Add(bar);
                col.Children.Add(nameLabel);
                col.Children.Add(detailLabel);

                BarChartPanel.Children.Add(col);
            }
        }

        private async void ExportCsv_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Student Name,LIN,Class,Expected,Paid,Balance,Status,Days Since Enrollment,Term");
                foreach (var d in _vm.Defaulters)
                {
                    sb.AppendLine($"\"{d.StudentName}\",\"{d.AdmissionNumber}\",\"{d.ClassName}\",{d.ExpectedAmount},{d.PaidAmount},{d.Balance},\"{d.Status}\",{d.DaysSinceEnrollment},\"{d.TermName}\"");
                }

                var folder = KnownFolders.DocumentsLibrary;
                var file = await folder.CreateFileAsync($"Defaulters_{DateTime.Now:yyyyMMdd_HHmmss}.csv", CreationCollisionOption.GenerateUniqueName);
                await FileIO.WriteTextAsync(file, sb.ToString(), Windows.Storage.Streams.UnicodeEncoding.Utf8);

                var dialog = new ContentDialog
                {
                    Title = "Export Complete",
                    Content = $"Saved {_vm.Defaulters.Count} records to:\n{file.Path}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Export Failed",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
