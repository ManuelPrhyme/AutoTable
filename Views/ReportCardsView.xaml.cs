using AutoTable.Models;
using AutoTable.Reports.ReportCards;
using AutoTable.Services;
using AutoTable.ViewModels;
using AutoTable.Views.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Printing;
using Windows.Storage.Streams;

namespace AutoTable.Views
{
    public sealed partial class ReportCardsView : Page
    {
        private PrintDocument? _printDocument;
        private IPrintDocumentSource? _printDocumentSource;

        /// <summary>Holds the A4 sheets to print — one page per student.</summary>
        private readonly List<UIElement> _pages = new();

        // ── Print-modal controls and state (direct-to-printer flow) ──
        private ComboBox? _printerCombo;
        private NumberBox? _copiesBox;
        private TextBlock? _dialogStatusText;
        private Viewbox? _previewViewbox;
        private List<PrinterChoice> _printerChoices = new();
        private bool _printInProgress;

        /// <summary>
        /// A printer shown in the print modal. Either a real configured printer
        /// (PrinterName set) or the "System print dialog…" option (UseSystemDialog).
        /// </summary>
        private sealed class PrinterChoice
        {
            public string DisplayName { get; init; } = string.Empty;
            public string PrinterName { get; init; } = string.Empty;
            public bool UseSystemDialog { get; init; }
        }

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
                var printTask = args.Request.CreatePrintTask("Report Card", requestArgs =>
                {
                    requestArgs.SetSource(_printDocumentSource);
                });
                ConfigurePrintTaskOptions(printTask.Options);
            }
            finally { def.Complete(); }
        }

        /// <summary>
        /// Applies sensible defaults (A4 portrait, color) so output is consistent on any
        /// installed printer and on "Microsoft Print to PDF". Individual printers may not
        /// support every option, so each is set best-effort.
        /// </summary>
        private static void ConfigurePrintTaskOptions(PrintTaskOptions options)
        {
            try { options.Orientation = PrintOrientation.Portrait; } catch { }
            try { options.MediaSize = PrintMediaSize.IsoA4; } catch { }
            try { options.ColorMode = PrintColorMode.Color; } catch { }
        }

        /// <summary>
        /// Opens the Windows print dialog, where the user picks ANY installed printer or
        /// "Microsoft Print to PDF" (Windows then asks for a save location). Shows a
        /// helpful message instead of failing silently when the dialog can't open.
        /// </summary>
        private async Task ShowPrintDialogAsync()
        {
            var opened = await PrintManager.ShowPrintUIAsync();
            if (!opened)
            {
                await new ContentDialog
                {
                    Title = "Printing unavailable",
                    Content = "Windows could not open the print dialog. Make sure at least one printer (or \"Microsoft Print to PDF\") is installed and enabled, then try again.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            else
            {
                // AppServices.Toasts.Show("Print", "The print dialog is ready. Choose a printer or \"Microsoft Print to PDF\" to finish.");
            }
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
            var rowList = rows.ToList();
            var total = rowList.Count;
            var list = new List<ReportCardSheetView>();

            // Show progress bar only for batch operations (2+ students)
            bool showProgress = total > 1;
            if (showProgress)
            {
                ProgressCard.Visibility = Visibility.Visible;
                ProgressRing.Value = 0;
                ProgressText.Text = $"Generating report cards...";
                ProgressDetail.Text = $"0 / {total} students";
            }

            for (int i = 0; i < total; i++)
            {
                if (showProgress)
                {
                    ProgressRing.Value = (double)i / total * 100;
                    ProgressDetail.Text = $"{i + 1} / {total} students  —  {rowList[i].StudentName}";
                }
                list.Add(await BuildSheetAsync(rowList[i]));
            }

            if (showProgress)
            {
                ProgressRing.Value = 100;
                ProgressText.Text = $"Done — {total} report card(s) ready";
                ProgressDetail.Text = "";
            }

            return list;
        }

        /// <summary>Shows a dialog to edit the head teacher comment for a student before printing.</summary>
        private async Task<bool> PromptHeadTeacherCommentAsync(ReportCardRow row)
        {
            var service = AppServices.DataService;
            if (service == null) return true;

            // Resolve the selected term to its DB id (null when "All")
            int? termId = null;
            if (!string.IsNullOrWhiteSpace(ViewModel.SelectedTerm) && ViewModel.SelectedTerm != "All")
            {
                var termLookups = await service.GetTermLookupsAsync();
                var match = termLookups.FirstOrDefault(t =>
                    string.Equals(t.Name, ViewModel.SelectedTerm, StringComparison.OrdinalIgnoreCase));
                termId = match?.Id;
            }

            // Load existing comment (scoped to the selected term)
            var settings = await service.GetSchoolSettingsAsync();
            var existingComment = await service.GetHeadTeacherCommentAsync(row.StudentId, termId);

            var commentBox = new TextBox
            {
                Text = existingComment,
                PlaceholderText = "Enter the head teacher's comment for this student...",
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100,
                MaxHeight = 200,
                AcceptsReturn = true,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var stack = new StackPanel
            {
                Spacing = 12,
                Width = 500
            };
            stack.Children.Add(new TextBlock
            {
                Text = $"Student: {row.StudentName}  •  Class: {row.ClassName}",
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 100, 100))
            });
            stack.Children.Add(new TextBlock
            {
                Text = "Head Teacher's Comment",
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            });
            stack.Children.Add(commentBox);
            if (!string.IsNullOrEmpty(settings.HeadTeacherName))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"Head Teacher: {settings.HeadTeacherName}",
                    FontSize = 12,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 120, 120, 120))
                });
            }

            var dialog = new ContentDialog
            {
                Title = "Head Teacher Comment",
                Content = stack,
                PrimaryButtonText = "Save & Continue",
                CloseButtonText = "Skip",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await service.SaveHeadTeacherCommentAsync(row.StudentId, termId, commentBox.Text, settings.HeadTeacherName);
            }
            return true;
        }

        private async void ViewReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: ReportCardRow row })
            {
                var sheets = await BuildSheetsAsync(new[] { row });
                await ShowPreviewAndPrintAsync(sheets, $"Report Card — {row.StudentName}", new[] { row });
            }
        }

        private async void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: ReportCardRow row })
            {
                try
                {
                    var sheets = await BuildSheetsAsync(new[] { row });
                    await ShowPreviewAndPrintAsync(sheets, $"Report Card — {row.StudentName}", new[] { row });
                }
                finally { HideProgress(); }
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

            try
            {
                var rows = ViewModel.ReportCards.ToList();
                var sheets = await BuildSheetsAsync(rows);
                await ShowPreviewAndPrintAsync(sheets, $"Report Cards — {sheets.Count} students", rows);
            }
            finally { HideProgress(); }
        }

        private async void PrintAll_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ReportCards.Count == 0)
            {
                await new ContentDialog
                {
                    Title = "Export All",
                    Content = "No students loaded. Adjust filters and try again.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                return;
            }

            var rows = ViewModel.ReportCards.ToList();

            try
            {
                // Use folder picker to save each student as a separate PDF
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                var hwnd = global::WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                global::WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                folderPicker.FileTypeFilter.Add("*");

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder == null) return;

                var service = AppServices.DataService;
                int saved = 0;

                ProgressCard.Visibility = Visibility.Visible;
                ProgressText.Text = $"Exporting {rows.Count} report cards...";
                ProgressRing.Value = 0;

                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    ProgressRing.Value = (double)i / rows.Count * 100;
                    ProgressDetail.Text = $"{i + 1} / {rows.Count}  —  {row.StudentName}";

                    ReportCardSheetModel? sd = null;
                    try { if (service != null) sd = await service.GetReportCardSheetAsync(row.StudentName, row.ClassName, ViewModel.SelectedTerm); } catch { }
                    var pdfBytes = ReportCardPdfGenerator.GeneratePdf(new[] { row }, new[] { sd });

                    var safeName = string.Join("_", row.StudentName.Split(System.IO.Path.GetInvalidFileNameChars())).Replace(" ", "_");
                    var fileName = $"ReportCard_{safeName}_{row.ClassName}.pdf";
                    var file = await folder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.GenerateUniqueName);
                    await Windows.Storage.FileIO.WriteBytesAsync(file, pdfBytes);
                    saved++;
                }

                ProgressRing.Value = 100;
                ProgressText.Text = $"Done — {saved} PDF(s) exported";
                ProgressDetail.Text = $"Saved to: {folder.Path}";

                await new ContentDialog
                {
                    Title = "PDF Exported",
                    Content = $"{saved} report card(s) saved as separate files to:\n{folder.Path}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                await new ContentDialog { Title = "Export failed", Content = ex.Message, CloseButtonText = "OK", XamlRoot = this.XamlRoot }.ShowAsync();
            }
            finally { HideProgress(); }
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

            var rows = ViewModel.ReportCards.ToList();

            try
            {
                // Show save file picker
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                var hwnd = global::WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                global::WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("PDF Document", new List<string> { ".pdf" });
                savePicker.SuggestedFileName = $"ReportCards_{ViewModel.SelectedClass}_{ViewModel.SelectedTerm}".Replace(" ", "_");

                var file = await savePicker.PickSaveFileAsync();
                if (file == null) return; // user cancelled

                ProgressCard.Visibility = Visibility.Visible;
                ProgressText.Text = "Generating PDF...";
                ProgressRing.Value = 0;
                ProgressDetail.Text = $"0 / {rows.Count} students";

                var service = AppServices.DataService;
                // Pre-load sheet data for all students
                var sheetDataList = new List<ReportCardSheetModel?>();
                for (int i = 0; i < rows.Count; i++)
                {
                    ProgressRing.Value = (double)i / rows.Count * 100;
                    ProgressDetail.Text = $"Loading {i + 1} / {rows.Count}  —  {rows[i].StudentName}";
                    ReportCardSheetModel? sd = null;
                    try { if (service != null) sd = await service.GetReportCardSheetAsync(rows[i].StudentName, rows[i].ClassName, ViewModel.SelectedTerm); } catch { }
                    sheetDataList.Add(sd);
                }

                var pdfBytes = ReportCardPdfGenerator.GeneratePdf(rows, sheetDataList);

                await Windows.Storage.FileIO.WriteBytesAsync(file, pdfBytes);

                ProgressRing.Value = 100;
                ProgressText.Text = $"Done — {rows.Count} report card(s) exported";
                ProgressDetail.Text = $"Saved to: {file.Path}";

                await new ContentDialog
                {
                    Title = "PDF Exported",
                    Content = $"{rows.Count} report card(s) saved to:\n{file.Path}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Export failed",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            finally
            {
                HideProgress();
            }
        }

        private async void PreviewFixture_Click(object sender, RoutedEventArgs e)
        {
            await ShowFixturePreviewAsync(ReportCardFixture.CreateSampleReport(), "Test Report Card");
        }

        private async void StressTestPreview_Click(object sender, RoutedEventArgs e)
        {
            await ShowFixturePreviewAsync(ReportCardFixture.CreateStressTestReport(), "Stress Test Report Card");
        }

        private async Task ShowFixturePreviewAsync(ReportCardData data, string title)
        {
            try
            {
                // Convert fixture data to the XAML preview model
                var sheetModel = ReportCardAdapter.ToSheetModel(data);
                var sheetView = new ReportCardSheetView { DataContext = sheetModel };

                // Build the same preview layout used by the print modal
                var previewContent = BuildPrintPreviewContent(sheetView, 1);

                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = previewContent,
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot,
                    Width = 1100,
                    Height = 780
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Preview Error",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }

        private void HideProgress()
        {
            ProgressCard.Visibility = Visibility.Collapsed;
            ProgressRing.Value = 0;
        }

        private async Task ShowMessageAsync(string title, string content)
        {
            await new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
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
                await ShowPrintDialogAsync();
            }
            finally { UnregisterForPrinting(); }
        }

        private async Task ShowPreviewAndPrintAsync(IReadOnlyList<ReportCardSheetView> sheets, string title, IReadOnlyList<ReportCardRow>? rowsForPdf = null)
        {
            if (sheets.Count == 0) return;

            // Left: scaled A4 preview, Right: printer config
            var previewContent = BuildPrintPreviewContent(sheets[0], sheets.Count);
            _printInProgress = false;

            var dialog = new ContentDialog
            {
                Title = title,
                Content = previewContent,
                PrimaryButtonText = "Print",
                SecondaryButtonText = rowsForPdf != null ? "Save PDF" : null,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Width = 1100,
                Height = 780
            };

            // Keep the dialog open while pages render and get sent to the chosen printer so
            // the user sees live status; close it manually when the work finishes.
            // (WinUI's ContentDialog has no IsOpen getter — track it via Opened/Closing.)
            bool dialogOpen = false;
            dialog.Opened += (_, _) => dialogOpen = true;
            dialog.Closing += (_, _) => dialogOpen = false;

            dialog.PrimaryButtonClick += async (s, e) =>
            {
                e.Cancel = true; // dismiss manually after printing completes
                if (_printInProgress) return;
                _printInProgress = true;
                dialog.IsPrimaryButtonEnabled = false;

                try
                {
                    var choice = (_printerCombo?.SelectedItem as PrinterChoice)
                        ?? _printerChoices.FirstOrDefault(c => c.UseSystemDialog)
                        ?? new PrinterChoice { DisplayName = "System print dialog…", UseSystemDialog = true };
                    int copies = (int)(_copiesBox?.Value ?? 1);

                    if (choice.UseSystemDialog)
                    {
                        // The OS chooser is its own top-level UI — close the modal first.
                        if (dialogOpen) dialog.Hide();
                        await Task.Yield();
                        RegisterForPrinting(sheets);
                        await ShowPrintDialogAsync();
                    }
                    else
                    {
                        bool ok = await PrintToPrinterAsync(sheets, choice.PrinterName, copies);
                        if (dialogOpen) dialog.Hide();
                        if (ok)
                        {
                            string targetName = string.IsNullOrWhiteSpace(choice.PrinterName)
                                ? "your default printer" : choice.PrinterName;
                            await new ContentDialog
                            {
                                Title = "Print complete",
                                Content = $"{sheets.Count} page(s) sent to {targetName}.",
                                CloseButtonText = "OK",
                                XamlRoot = this.XamlRoot
                            }.ShowAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (dialogOpen) dialog.Hide();
                    await ShowMessageAsync("Print failed", ex.Message);
                }
                finally
                {
                    UnregisterForPrinting();
                    _printInProgress = false;
                    dialog.IsPrimaryButtonEnabled = true;
                }
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Secondary && rowsForPdf != null)
            {
                await ExportPdfFromSheetsAsync(rowsForPdf, sheets);
            }
        }

        /// <summary>
        /// Prints the given A4 sheets straight to the chosen printer without the OS dialog:
        /// each sheet is rasterized at 2x resolution with RenderTargetBitmap, converted to a
        /// GDI bitmap, and spooled to the selected printer (hands copies to the driver too).
        /// Returns true when the job was submitted successfully.
        /// </summary>
        private async Task<bool> PrintToPrinterAsync(IReadOnlyList<ReportCardSheetView> sheets, string printerName, int copies)
        {
            if (sheets.Count == 0) return false;
            var pages = new List<Bitmap>();
            const int renderScale = 2;

            try
            {
                // Every sheet must live in the page's visual tree before it can be
                // rasterized — RenderTargetBitmap cannot capture Popup (ContentDialog)
                // content. Take the preview sheet out of the dialog's Viewbox and park
                // all sheets in the off-screen RenderHost instead.
                if (_previewViewbox != null)
                    _previewViewbox.Child = null;
                foreach (var sheet in sheets)
                {
                    if (!RenderHost.Children.Contains(sheet))
                        RenderHost.Children.Add(sheet);
                }
                RenderHost.UpdateLayout();
                await Task.Yield();

                for (int i = 0; i < sheets.Count; i++)
                {
                    if (_dialogStatusText != null)
                        _dialogStatusText.Text = $"Rendering page {i + 1} of {sheets.Count}…";

                    var rtb = new RenderTargetBitmap();
                    await rtb.RenderAsync(sheets[i],
                        (int)(sheets[i].Width * renderScale),
                        (int)(sheets[i].Height * renderScale));

                    var pixelBuffer = await rtb.GetPixelsAsync();
                    var reader = DataReader.FromBuffer(pixelBuffer);
                    var pixelData = new byte[pixelBuffer.Length];
                    reader.ReadBytes(pixelData);

                    pages.Add(PrinterService.BitmapFromBgraPixels(pixelData, rtb.PixelWidth, rtb.PixelHeight));
                }

                string targetName = string.IsNullOrWhiteSpace(printerName) ? "your default printer" : printerName;
                if (_dialogStatusText != null)
                    _dialogStatusText.Text = $"Sending {sheets.Count} page(s) to {targetName}…";

                bool ok = PrinterService.PrintBitmaps(printerName, (short)Math.Max(1, copies), pages, out var error);
                if (!ok)
                {
                    await ShowMessageAsync("Print failed", error);
                    return false;
                }

                ViewModel.StatusMessage = $"Sent {sheets.Count} page(s) to {targetName}.";
                return true;
            }
            finally
            {
                foreach (var page in pages) page.Dispose();
                pages.Clear();
                foreach (var sheet in sheets)
                    RenderHost.Children.Remove(sheet);
                if (_dialogStatusText != null) _dialogStatusText.Text = string.Empty;
            }
        }

        /// <summary>
        /// Generates a PDF from the already-built sheet views and saves it via the file picker.
        /// </summary>
        private async Task ExportPdfFromSheetsAsync(IReadOnlyList<ReportCardRow> rows, IReadOnlyList<ReportCardSheetView> sheets)
        {
            try
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                var hwnd = global::WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                global::WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("PDF Document", new List<string> { ".pdf" });
                savePicker.SuggestedFileName = rows.Count == 1
                    ? $"ReportCard_{rows[0].StudentName.Replace(" ", "_")}_{rows[0].ClassName}"
                    : $"ReportCards_{ViewModel.SelectedClass}_{ViewModel.SelectedTerm}".Replace(" ", "_");

                var file = await savePicker.PickSaveFileAsync();
                if (file == null) return;

                ProgressCard.Visibility = Visibility.Visible;
                ProgressText.Text = "Generating PDF...";
                ProgressRing.Value = 100;
                ProgressDetail.Text = $"{rows.Count} report card(s)";

                // Extract the ReportCardSheetModel from each view's DataContext
                var sheetModels = sheets.Select(s => s.DataContext as ReportCardSheetModel).ToList();
                var pdfBytes = ReportCardPdfGenerator.GeneratePdf(rows, sheetModels);

                await Windows.Storage.FileIO.WriteBytesAsync(file, pdfBytes);

                ProgressText.Text = $"Done — {rows.Count} report card(s) exported";
                ProgressDetail.Text = $"Saved to: {file.Path}";

                await new ContentDialog
                {
                    Title = "PDF Saved",
                    Content = $"{rows.Count} report card(s) saved to:\n{file.Path}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Export failed",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
            finally { HideProgress(); }
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
            _previewViewbox = sheetViewbox;

            var previewGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 240, 240))
            };
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(sheetViewbox, 0);
            previewGrid.Children.Add(sheetViewbox);

            // Right panel: printer configuration — detect every configured printer so the
            // user can choose which one to print with (single student or a batch).
            var printerCombo = new ComboBox
            {
                Header = "Printer",
                MinWidth = 220,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var printers = PrinterService.GetInstalledPrinters().ToList();
            var defaultPrinter = PrinterService.GetDefaultPrinterName();
            _printerChoices = new List<PrinterChoice>();
            foreach (var printer in printers)
            {
                bool isDefault = string.Equals(printer, defaultPrinter, StringComparison.OrdinalIgnoreCase);
                _printerChoices.Add(new PrinterChoice
                {
                    DisplayName = isDefault ? $"{printer}  (Default)" : printer,
                    PrinterName = printer
                });
            }
            // Windows' own chooser stays available as an explicit option for familiarity.
            _printerChoices.Add(new PrinterChoice
            {
                DisplayName = "System print dialog…",
                UseSystemDialog = true
            });

            printerCombo.ItemsSource = _printerChoices;
            printerCombo.DisplayMemberPath = nameof(PrinterChoice.DisplayName);
            // Preselect the system default printer; otherwise fall back to the first choice.
            int defaultIndex = _printerChoices.FindIndex(c =>
                !c.UseSystemDialog && string.Equals(c.PrinterName, defaultPrinter, StringComparison.OrdinalIgnoreCase));
            printerCombo.SelectedIndex = Math.Max(0, defaultIndex);
            _printerCombo = printerCombo;

            var copiesBox = new NumberBox
            {
                Header = "Copies",
                Value = 1,
                Minimum = 1,
                Maximum = 99,
                MinWidth = 100,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _copiesBox = copiesBox;

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
            var statusText = new TextBlock
            {
                Text = printers.Count == 0
                    ? "No printers were detected — the system print dialog will be used."
                    : $"{printers.Count} printer(s) detected.",
                FontSize = 11,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 120, 120, 120)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            };
            _dialogStatusText = statusText;

            printerPanel.Children.Add(printerCombo);
            printerPanel.Children.Add(pageRangeText);
            printerPanel.Children.Add(copiesBox);
            printerPanel.Children.Add(statusText);
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
