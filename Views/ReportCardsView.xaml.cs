using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using AutoTable.Models;
using Microsoft.UI.Xaml.Printing;
using Windows.Graphics.Printing;

namespace AutoTable.Views
{
    public sealed partial class ReportCardsView : Page
    {
        private PrintDocument? _printDocument;
        private IPrintDocumentSource? _printDocumentSource;
        private UIElement? _elementToPrint;

        private void RegisterForPrinting(UIElement element)
        {
            UnregisterForPrinting();
            _elementToPrint = element;
            _printDocument = new PrintDocument();
            _printDocumentSource = _printDocument.DocumentSource;
            _printDocument.Paginate += PrintDocument_Paginate;
            _printDocument.GetPreviewPage += PrintDocument_GetPreviewPage;
            _printDocument.AddPages += PrintDocument_AddPages;
            PrintManager.GetForCurrentView().PrintTaskRequested += PrintTaskRequested;
        }

        private void UnregisterForPrinting()
        {
            if (_printDocument != null)
            {
                _printDocument.Paginate -= PrintDocument_Paginate;
                _printDocument.GetPreviewPage -= PrintDocument_GetPreviewPage;
                _printDocument.AddPages -= PrintDocument_AddPages;
                _printDocument = null;
                _printDocumentSource = null;
            }
            try { PrintManager.GetForCurrentView().PrintTaskRequested -= PrintTaskRequested; } catch { }
            _elementToPrint = null;
        }

        private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            var def = args.Request.GetDeferral();
            try
            {
                args.Request.CreatePrintTask("Report Card", requestArgs =>
                {
                    requestArgs.SetSource(_printDocumentSource);
                });
            }
            finally { def.Complete(); }
        }

        private void PrintDocument_Paginate(object? sender, PaginateEventArgs e)
        {
            _printDocument?.SetPreviewPageCount(1, PreviewPageCountType.Final);
        }

        private void PrintDocument_GetPreviewPage(object? sender, GetPreviewPageEventArgs e)
        {
            if (_elementToPrint != null)
                _printDocument?.SetPreviewPage(1, _elementToPrint);
        }

        private void PrintDocument_AddPages(object? sender, AddPagesEventArgs e)
        {
            if (_elementToPrint != null)
                _printDocument?.AddPage(_elementToPrint);
            _printDocument?.AddPagesComplete();
        }
        public ReportCardsViewModel ViewModel { get; } = new();
        public ReportCardsView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private async void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ReportCardRow row)
            {
                // Simple A4 preview (approx pixels @96dpi: 794x1123)
                var previewGrid = new Grid { Width = 794, Height = 1123, Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SurfaceWhiteBrush"] };
                var stack = new StackPanel { Padding = new Thickness(40), Spacing = 12 };
                stack.Children.Add(new TextBlock { Text = "School Name", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
                stack.Children.Add(new TextBlock { Text = "Report Card", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                stack.Children.Add(new TextBlock { Text = $"Student: {row.StudentName}", FontSize = 14 });
                stack.Children.Add(new TextBlock { Text = $"LIN: {row.AdmissionNumber}", FontSize = 14 });
                stack.Children.Add(new TextBlock { Text = $"Class: {row.ClassName}", FontSize = 14 });
                stack.Children.Add(new TextBlock { Text = $"Average: {row.Average}", FontSize = 14 });
                previewGrid.Children.Add(stack);

                var dialog = new ContentDialog
                {
                    Title = $"Print Preview - {row.StudentName}",
                    Content = new ScrollViewer { Content = previewGrid, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
                    PrimaryButtonText = "Print",
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot,
                    Width = 900,
                    Height = 700
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        // Register the preview grid for printing and show the system print UI
                        RegisterForPrinting(previewGrid);
                        await PrintManager.ShowPrintUIAsync();
                    }
                    finally
                    {
                        UnregisterForPrinting();
                    }
                }
            }
        }

        private async void PrintAll_Click(object sender, RoutedEventArgs e)
        {
            // Build an A4 preview containing multiple report rows
            var previewGrid = new Grid { Width = 794, Height = 1123, Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SurfaceWhiteBrush"] };
            var stack = new StackPanel { Padding = new Thickness(24), Spacing = 8 };
            stack.Children.Add(new TextBlock { Text = "School Name", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
            stack.Children.Add(new TextBlock { Text = "Report Cards", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

            foreach (var row in ViewModel.ReportCards)
            {
                var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
                rowPanel.Children.Add(new TextBlock { Text = row.Rank.ToString(), Width = 36 });
                rowPanel.Children.Add(new TextBlock { Text = row.StudentName, Width = 220 });
                rowPanel.Children.Add(new TextBlock { Text = row.AdmissionNumber, Width = 120 });
                rowPanel.Children.Add(new TextBlock { Text = row.ClassName, Width = 80 });
                rowPanel.Children.Add(new TextBlock { Text = row.Average.ToString("F1"), Width = 60 });
                rowPanel.Children.Add(new TextBlock { Text = row.Status, Width = 140 });
                stack.Children.Add(rowPanel);
            }

            previewGrid.Children.Add(stack);

            var dialog = new ContentDialog
            {
                Title = "Print Preview - All Report Cards",
                Content = new ScrollViewer { Content = previewGrid, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
                PrimaryButtonText = "Print",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Width = 1000,
                Height = 700
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    RegisterForPrinting(previewGrid);
                    await PrintManager.ShowPrintUIAsync();
                }
                finally
                {
                    UnregisterForPrinting();
                }
            }
        }
    }
}
