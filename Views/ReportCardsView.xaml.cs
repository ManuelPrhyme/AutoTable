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

        private async void PrintAll_Click(object sender, RoutedEventArgs e)
        {
            var sheets = await BuildSheetsAsync(ViewModel.ReportCards);
            await ShowPreviewAndPrintAsync(sheets, $"Print Preview — {sheets.Count} report cards");
        }

        private async Task ShowPreviewAndPrintAsync(IReadOnlyList<ReportCardSheetView> sheets, string title)
        {
            if (sheets.Count == 0) return;

            var dialog = new ContentDialog
            {
                Title = title,
                Content = new ScrollViewer
                {
                    Content = sheets[0],
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                },
                PrimaryButtonText = "Print",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Width = 940,
                Height = 720
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
    }
}
