using AutoTable.Models;
using AutoTable.ViewModels;
using AutoTable.Views.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Printing;

namespace AutoTable.Views
{
    public sealed partial class ReportCardsView : Page
    {
        private PrintDocument? _printDocument;
        private IPrintDocumentSource? _printDocumentSource;

        /// <summary>Holds the A4 sheets to print — one page per student.</summary>
        private readonly List<UIElement> _pages = new();

        private void RegisterForPrinting(IEnumerable<UIElement> pages)
        {
            UnregisterForPrinting();
            _pages.AddRange(pages);
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
            _pages.Clear();
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
            if (_pages.Count > 0)
                _printDocument?.SetPreviewPageCount(_pages.Count, PreviewPageCountType.Final);
        }

        private void PrintDocument_GetPreviewPage(object? sender, GetPreviewPageEventArgs e)
        {
            if (e.PageNumber >= 1 && e.PageNumber <= _pages.Count)
                _printDocument?.SetPreviewPage(e.PageNumber, _pages[e.PageNumber - 1]);
        }

        private void PrintDocument_AddPages(object? sender, AddPagesEventArgs e)
        {
            foreach (var page in _pages)
                _printDocument?.AddPage(page);
            _printDocument?.AddPagesComplete();
        }
        public ReportCardsViewModel ViewModel { get; } = new();
        public ReportCardsView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private async Task<ReportCardSheetView> BuildSheetAsync(ReportCardRow row)
        {
            var service = AppServices.DataService;
            if (service == null)
                return new ReportCardSheetView { DataContext = null };
            var data = await service.GetReportCardSheetAsync(
                row.StudentName, row.ClassName, ViewModel.SelectedTerm);
            return new ReportCardSheetView { DataContext = data };
        }

        private async Task<IReadOnlyList<ReportCardSheetView>> BuildSheetsAsync(IEnumerable<ReportCardRow> rows)
        {
            var list = new List<ReportCardSheetView>();
            foreach (var row in rows) list.Add(await BuildSheetAsync(row));
            return list;
        }

        private async void ViewReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: ReportCardRow row })
            {
                var sheets = await BuildSheetsAsync(new[] { row });
                await ShowPreviewAndPrintAsync(sheets, $"Report Card — {row.StudentName}");
            }
        }

        private async void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: ReportCardRow row })
            {
                var sheets = await BuildSheetsAsync(new[] { row });
                await ShowPreviewAndPrintAsync(sheets, $"Print Preview — {row.StudentName}");
            }
        }

        private async void GenerateAll_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ReportCards.Count == 0)
            {
                await new ContentDialog
                {
                    Title = "Generate All",
                    Content = "No students loaded. Adjust filters and try again.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                return;
            }

            var sheets = await BuildSheetsAsync(ViewModel.ReportCards);
            await ShowPreviewAndPrintAsync(sheets, $"Report Cards — {sheets.Count} students");
        }

        private async void PrintAll_Click(object sender, RoutedEventArgs e)
        {
            var sheets = await BuildSheetsAsync(ViewModel.ReportCards);
            await ShowPreviewAndPrintAsync(sheets, $"Print Preview — {sheets.Count} report cards");
        }

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ReportCards.Count == 0)
            {
                await new ContentDialog
                {
                    Title = "Export PDF",
                    Content = "No students loaded. Adjust filters and try again.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                return;
            }

            var sheets = await BuildSheetsAsync(ViewModel.ReportCards);
            if (sheets.Count == 0) return;

            // Show info dialog: user should select "Microsoft Print to PDF" in the print dialog
            var infoDialog = new ContentDialog
            {
                Title = "Export as PDF",
                Content = $"{sheets.Count} report card(s) ready. In the print dialog, select \"Microsoft Print to PDF\" as the printer, then click Print to save as PDF.",
                PrimaryButtonText = "Open Print Dialog",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var infoResult = await infoDialog.ShowAsync();
            if (infoResult != ContentDialogResult.Primary) return;

            try
            {
                RegisterForPrinting(sheets);
                await PrintManager.ShowPrintUIAsync();
            }
            finally { UnregisterForPrinting(); }
        }

        private async void MidTermSlips_Click(object sender, RoutedEventArgs e)
        {
            var service = AppServices.DataService;
            if (service == null) return;

            var stream = ViewModel.SelectedStream;
            if (stream == "None" || string.IsNullOrWhiteSpace(stream))
                stream = null;

            var slips = await service.GetMidTermSlipsAsync(
                ViewModel.SelectedClass, ViewModel.SelectedTerm, stream);

            if (slips.Count == 0)
            {
                await new ContentDialog
                {
                    Title = "Mid-Term Slips",
                    Content = "No students found for the selected filters.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                return;
            }

            // Build compact slip views — 3 per A4 page
            var slipViews = slips.Select(s =>
            {
                var view = new MidTermSlipView { DataContext = s };
                return view;
            }).ToList();

            // Show first slip in preview dialog
            var dialog = new ContentDialog
            {
                Title = $"Mid-Term Slips — {slips.Count} students",
                Content = new ScrollViewer
                {
                    Content = slipViews[0],
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                },
                PrimaryButtonText = "Print All",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Width = 860,
                Height = 500
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                RegisterForPrinting(slipViews);
                await PrintManager.ShowPrintUIAsync();
            }
            finally { UnregisterForPrinting(); }
        }

        private async Task ShowPreviewAndPrintAsync(IReadOnlyList<ReportCardSheetView> sheets, string title)
        {
            if (sheets.Count == 0) return;

            // Left: scaled A4 preview, Right: printer config
            var previewContent = BuildPrintPreviewContent(sheets[0], sheets.Count);

            var dialog = new ContentDialog
            {
                Title = title,
                Content = previewContent,
                PrimaryButtonText = "Print",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Width = 1100,
                Height = 780
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                RegisterForPrinting(sheets);
                await PrintManager.ShowPrintUIAsync();
            }
            finally { UnregisterForPrinting(); }
        }

        private Grid BuildPrintPreviewContent(ReportCardSheetView sheet, int totalSheets)
        {
            // Left panel: scaled A4 preview
            // Wrap in a Grid with fixed row height so Viewbox has a constraint to scale against
            var sheetViewbox = new Viewbox
            {
                Child = sheet,
                Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };

            var previewGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 240, 240))
            };
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(sheetViewbox, 0);
            previewGrid.Children.Add(sheetViewbox);

            // Right panel: printer configuration
            var printerCombo = new ComboBox
            {
                Header = "Printer",
                MinWidth = 220,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            printerCombo.Items.Add("Default Printer");
            printerCombo.SelectedIndex = 0;

            var copiesBox = new NumberBox
            {
                Header = "Copies",
                Value = 1,
                Minimum = 1,
                Maximum = 99,
                MinWidth = 100,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var pageRangeText = new TextBlock
            {
                Text = $"Pages: 1 \u2013 {totalSheets}",
                FontSize = 12,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 85, 85, 85)),
                Margin = new Thickness(0, 0, 0, 4)
            };

            var summaryText = new TextBlock
            {
                Text = $"{totalSheets} page(s) ready to print",
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 46, 134, 193)),
                Margin = new Thickness(0, 12, 0, 0)
            };

            var printerPanel = new StackPanel
            {
                Spacing = 12,
                Width = 260,
                Margin = new Thickness(16, 8, 8, 8)
            };
            printerPanel.Children.Add(new TextBlock
            {
                Text = "Print Settings",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 31, 56, 100)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            printerPanel.Children.Add(printerCombo);
            printerPanel.Children.Add(pageRangeText);
            printerPanel.Children.Add(copiesBox);
            printerPanel.Children.Add(summaryText);

            // Separator line
            var separator = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Width = 1,
                Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 200, 200)),
                Margin = new Thickness(4, 0, 4, 0)
            };
            Grid.SetRowSpan(separator, 1);

            // Main layout: preview left, separator middle, settings right
            var layout = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                RowSpacing = 0
            };
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });

            Grid.SetRow(previewGrid, 0);
            Grid.SetColumn(previewGrid, 0);
            Grid.SetRow(separator, 0);
            Grid.SetColumn(separator, 1);
            Grid.SetRow(printerPanel, 0);
            Grid.SetColumn(printerPanel, 2);

            layout.Children.Add(previewGrid);
            layout.Children.Add(separator);
            layout.Children.Add(printerPanel);

            return layout;
        }
    }
}
