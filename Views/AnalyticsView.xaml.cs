using AutoTable.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace AutoTable.Views
{
    public sealed partial class AnalyticsView : Page
    {
        private readonly AnalyticsViewModel _vm;

        public AnalyticsView()
        {
            InitializeComponent();
            _vm = new AnalyticsViewModel();
            DataContext = _vm;
            _vm.ClassBreakdown.CollectionChanged += (_, _) => BuildBarChart();
            _vm.TermTrend.CollectionChanged += (_, _) => BuildTrendChart();
        }

        private void BuildBarChart()
        {
            BarChartPanel.Children.Clear();
            if (_vm.ClassBreakdown.Count == 0) return;

            var maxAvg = _vm.ClassBreakdown.Max(c => c.Average);
            if (maxAvg <= 0) maxAvg = 100;

            foreach (var cls in _vm.ClassBreakdown)
            {
                var barHeight = (cls.Average / maxAvg) * 160;
                if (barHeight < 4 && cls.Average > 0) barHeight = 4;

                var brush = cls.Average >= 70
                    ? (Brush)Resources["SuccessGreenBrush"]
                    : cls.Average >= 50
                        ? (Brush)Resources["WarningAmberBrush"]
                        : (Brush)Resources["DangerRedBrush"];

                var bar = new Border
                {
                    Width = 32,
                    Height = barHeight,
                    Background = brush,
                    CornerRadius = new CornerRadius(4),
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                var avgLabel = new TextBlock
                {
                    Text = $"{cls.Average:F0}%",
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = (Brush)Resources["TextSecondaryBrush"],
                    Margin = new Thickness(0, 2, 0, 0)
                };

                var nameLabel = new TextBlock
                {
                    Text = cls.ClassName,
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = (Brush)Resources["TextSecondaryBrush"]
                };

                var col = new StackPanel { Spacing = 4 };
                col.Children.Add(avgLabel);
                col.Children.Add(bar);
                col.Children.Add(nameLabel);

                BarChartPanel.Children.Add(col);
            }
        }

        private void BuildTrendChart()
        {
            TrendChartCanvas.Children.Clear();
            if (_vm.TermTrend.Count == 0) return;

            var points = _vm.TermTrend.ToList();
            var maxAvg = points.Max(p => p.Average);
            var minAvg = points.Min(p => p.Average);
            if (maxAvg <= minAvg) maxAvg = minAvg + 10;

            var canvasWidth = 400.0;
            var canvasHeight = 170.0;
            var stepX = points.Count > 1 ? canvasWidth / (points.Count - 1) : 0;

            // Draw grid lines
            for (int g = 0; g <= 4; g++)
            {
                var y = (canvasHeight / 4) * g;
                var gridLine = new Line
                {
                    X1 = 0, Y1 = y, X2 = canvasWidth, Y2 = y,
                    Stroke = (Brush)Resources["BorderLightBrush"],
                    StrokeThickness = 1,
                    StrokeDashArray = { 4, 3 }
                };
                TrendChartCanvas.Children.Add(gridLine);
            }

            // Build polyline points
            var polyline = new Polyline
            {
                Stroke = (Brush)Resources["PrimaryBlueBrush"],
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round
            };

            for (int i = 0; i < points.Count; i++)
            {
                var x = stepX * i;
                var normalizedY = (points[i].Average - minAvg) / (maxAvg - minAvg);
                var y = canvasHeight - (normalizedY * (canvasHeight - 20)) - 10;

                polyline.Points.Add(new Windows.Foundation.Point(x, y));

                // Dot
                var dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = (Brush)Resources["PrimaryBlueBrush"],
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                Canvas.SetLeft(dot, x - 5);
                Canvas.SetTop(dot, y - 5);
                TrendChartCanvas.Children.Add(dot);

                // Label below
                var label = new TextBlock
                {
                    Text = points[i].Term,
                    FontSize = 10,
                    Foreground = (Brush)Resources["TextSecondaryBrush"],
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                Canvas.SetLeft(label, x - 20);
                Canvas.SetTop(label, canvasHeight + 2);
                TrendChartCanvas.Children.Add(label);

                // Value above
                var val = new TextBlock
                {
                    Text = $"{points[i].Average:F1}%",
                    FontSize = 10,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = (Brush)Resources["PrimaryBlueBrush"],
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                Canvas.SetLeft(val, x - 15);
                Canvas.SetTop(val, y - 18);
                TrendChartCanvas.Children.Add(val);
            }

            TrendChartCanvas.Children.Add(polyline);
        }
    }
}
