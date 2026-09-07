using AutoTable.Models;
using AutoTable.Reports.ReportCards;
using AutoTable.Services;
using AutoTable.ViewModels;
using AutoTable.Views.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        // ── Assessment selection for report cards / marks slips ──
        // When set, only the assessment names in this set appear on printed
        // report cards / slips. Empty set = include everything.
        private HashSet<string> _includedAssessmentNames = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _includedAssessmentIds = new();

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

            // When the user explicitly selected assessments, keep only those rows
            // on the sheet (both the promotional and contributory result tables).
            if (data != null && _includedAssessmentIds.Count > 0)
            {
                data.PromotionalAssessments = data.PromotionalAssessments
                    .Where(r => AssessmentRowIncluded(r.AssessmentName, r.Subject))
                    .ToList();
                data.ContributoryAssessments = data.ContributoryAssessments
                    .Where(r => AssessmentRowIncluded(r.AssessmentName, r.Subject))
                    .ToList();
            }

            return new ReportCardSheetView { DataContext = data };
        }

        /// <summary>True when an assessment row (name + optional subject) is in the user's printed selection.</summary>
        private bool AssessmentRowIncluded(string assessmentName, string subject)
        {
            if (_includedAssessmentIds.Count == 0) return true;
            var key = string.IsNullOrWhiteSpace(subject) ? assessmentName : $"{assessmentName} — {subject}";
            return _includedAssessmentNames.Contains(key)
                || _includedAssessmentNames.Contains(assessmentName);
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
                    if (!await PromptReportCardAssessmentSelectionAsync()) return;
                    var sheets = await BuildSheetsAsync(new[] { row });
                    await ShowPreviewAndPrintAsync(sheets, $"Report Card — {row.StudentName}", new[] { row });
                    _ = AppServices.Audit.LogAsync("Reporting", "Print", "ReportCard",
                        row.StudentId.ToString(), row.StudentName,
                        $"Report card print flow opened for {row.StudentName}.", isSuccess: true);
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
                // Let the user pick which assessments to include on the report cards.
                if (!await PromptReportCardAssessmentSelectionAsync()) return;
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

        // ── Assessment selection helper (report cards) ──────────────────────────

        /// <summary>
        /// Shows a modal listing the contributory assessments (end-of-term / end-of-year)
        /// for the current class/term so the user can pick which to print on a report card.
        /// Returns true when a selection was confirmed.
        /// </summary>
        private async Task<bool> PromptReportCardAssessmentSelectionAsync()
        {
            var service = AppServices.DataService;
            if (service == null) return false;

            var className = ViewModel.SelectedClass;
            var termName = ViewModel.SelectedTerm;
            if (string.IsNullOrWhiteSpace(className) || string.Equals(className, "All", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(termName) || string.Equals(termName, "All", StringComparison.OrdinalIgnoreCase))
            {
                await new ContentDialog
                {
                    Title = "Select Filters First",
                    Content = "Pick a specific class and term before selecting assessments. \"All\" is not supported for report-card assessment selection.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                return false;
            }

            var all = new List<AssessmentItem>();
            try { all = (await service.GetAssessmentsAsync()).ToList(); }
            catch { all = new List<AssessmentItem>(); }

            // Only contributory assessments (end-of-term / end-of-year) belong on the card.
            var contributory = all
                .Where(a => a.PromotionRole != AssessmentPromotionRole.None)
                .OrderBy(a => a.Subject)
                .ThenBy(a => a.Name)
                .ToList();

            if (contributory.Count == 0)
            {
                await new ContentDialog
                {
                    Title = "No Contributory Assessments",
                    Content = "There are no assessments marked as contributory to the end-of-term or end-of-year result for this class/term. Mark an assessment as contributory first.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                return false;
            }

            var items = contributory.Select(a => new AssessmentOption
            {
                Id = a.Id,
                Name = a.Name,
                Subject = a.Subject,
                Display = string.IsNullOrWhiteSpace(a.Subject) ? a.Name : $"{a.Name} — {a.Subject}",
                IsSelected = _includedAssessmentIds.Count == 0 || _includedAssessmentIds.Contains(a.Id)
            }).ToList();

            var checkboxHost = new StackPanel { Spacing = 4, MaxHeight = 320 };
            foreach (var opt in items)
            {
                var chk = new CheckBox { Content = opt.Display, IsChecked = opt.IsSelected, Margin = new Thickness(2, 0, 0, 0) };
                chk.Click += (_, _) => opt.IsSelected = chk.IsChecked == true;
                checkboxHost.Children.Add(chk);
            }

            var selectAll = new CheckBox { Content = "Select all contributory assessments", Margin = new Thickness(0, 0, 0, 6) };
            selectAll.IsChecked = items.All(i => i.IsSelected);
            selectAll.Click += (_, _) =>
            {
                bool check = selectAll.IsChecked == true;
                foreach (var i in items) i.IsSelected = check;
                foreach (var child in checkboxHost.Children.OfType<CheckBox>()) child.IsChecked = check;
            };

            var stack = new StackPanel { Spacing = 8, Width = 470 };
            stack.Children.Add(new TextBlock
            {
                Text = "Choose which assessments to print on the report card. Only assessments that contribute to the end-of-term or end-of-year result are listed.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Opacity = 0.8
            });
            stack.Children.Add(selectAll);
            stack.Children.Add(new ScrollViewer
            {
                Content = checkboxHost,
                MaxHeight = 300,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            });

            var dialog = new ContentDialog
            {
                Title = "Select Assessments for Report Card",
                Content = stack,
                PrimaryButtonText = "OK — Show Print Preview",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
                Width = 520
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return false;

            var chosen = items.Where(i => i.IsSelected).ToList();
            if (chosen.Count == 0)
            {
                await new ContentDialog
                {
                    Title = "No Assessment Selected",
                    Content = "Select at least one assessment to include on the report cards.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                return false;
            }

            _includedAssessmentIds = chosen.Select(c => c.Id).ToHashSet();
            _includedAssessmentNames = chosen
                .Select(c => string.IsNullOrWhiteSpace(c.Subject) ? c.Name : $"{c.Name} — {c.Subject}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return true;
        }

        private sealed class AssessmentOption : INotifyPropertyChanged
        {
            public string Id { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
            public string Subject { get; init; } = string.Empty;
            public string Display { get; init; } = string.Empty;

            private bool _isSelected = true;
            public bool IsSelected
            {
                get => _isSelected;
                set { _isSelected = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); }
            }

            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        }

        private async void MidTermSlips_Click(object sender, RoutedEventArgs e)
        {
            var service = AppServices.DataService;
            if (service == null) return;

            // 1) Let the user choose which assessments to put on the marks slips (max 2).
            var selectedIds = await PromptMarksSlipAssessmentSelectionAsync();
            if (selectedIds == null) return; // cancelled

            var stream = ViewModel.SelectedStream;
            if (stream == "None" || string.IsNullOrWhiteSpace(stream))
                stream = null;

            var slips = await service.GetMidTermSlipsAsync(
                ViewModel.SelectedClass, ViewModel.SelectedTerm, stream, selectedIds);

            if (slips.Count == 0)
            {
                await new ContentDialog
                {
                    Title = "Marks Slips",
                    Content = "No students found for the selected filters.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                return;
            }

            // 2) Build A4 pages: 5 slips per sheet, each torn off at the dotted line.
            var pages = BuildMarksSlipA4Pages(slips);

            // 3) Preview the first page, then print all on request.
            var dialog = new ContentDialog
            {
                Title = $"Marks Slips — {slips.Count} students ({pages.Count} sheet(s))",
                Content = new ScrollViewer
                {
                    Content = pages.Count > 0 ? pages[0] : null,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                },
                PrimaryButtonText = "Print All",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Width = 860,
                Height = 560
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                RegisterForPrinting(pages);
                await ShowPrintDialogAsync();
            }
            finally { UnregisterForPrinting(); }
        }

        /// <summary>
        /// Shows a modal listing the class/term assessments so the user can pick which
        /// papers to print on the marks slips (max 2, per the school's slip format).
        /// Returns null when cancelled, an empty list when "All assessments" was chosen.
        /// </summary>
        private async Task<IReadOnlyList<string>?> PromptMarksSlipAssessmentSelectionAsync()
        {
            var service = AppServices.DataService;
            if (service == null) return new List<string>();

            var all = new List<AssessmentItem>();
            try { all = (await service.GetAssessmentsAsync()).ToList(); }
            catch { all = new List<AssessmentItem>(); }

            var className = ViewModel.SelectedClass;
            var candidates = all
                .Where(a =>
                    string.IsNullOrWhiteSpace(className) || string.Equals(className, "All", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a.ClassName, className, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a.ClassName, "All Classes", StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Subject)
                .ThenBy(a => a.Name)
                .ToList();

            if (candidates.Count == 0)
            {
                await new ContentDialog
                {
                    Title = "No Assessments",
                    Content = "There are no assessments for the current class/term. Create an assessment first so it can appear on the marks slips.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
                return new List<string>();
            }

                                    var checkboxes = new List<CheckBox>();
            foreach (var a in candidates)
            {
                var dbId = a.Id; // string GUID
                var label = string.IsNullOrWhiteSpace(a.Subject) ? a.Name : $"{a.Name} ({a.Subject})";
                var chk = new CheckBox { Content = label, Tag = dbId, Margin = new Thickness(2, 2, 0, 2) };
                chk.Click += (_, _) =>
                {
                    // Enforce the 2-assessment maximum.
                    if (checkboxes.Count(c => c.IsChecked == true) > 2)
                        chk.IsChecked = false;
                };
                checkboxes.Add(chk);
            }

            var allAssess = new CheckBox
            {
                Content = "All assessments",
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 6)
            };
            allAssess.Click += (_, _) =>
            {
                bool check = allAssess.IsChecked == true;
                foreach (var c in checkboxes) c.IsChecked = check;
            };

            var host = new StackPanel { Spacing = 2, MaxHeight = 320 };
            foreach (var c in checkboxes) host.Children.Add(c);

            var stack = new StackPanel { Spacing = 8, Width = 460 };
            stack.Children.Add(new TextBlock
            {
                Text = "Choose which assessments to print on the marks slips. You can select at most 2 assessments.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Opacity = 0.8
            });
            stack.Children.Add(allAssess);
            stack.Children.Add(new ScrollViewer
            {
                Content = host,
                MaxHeight = 300,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            });

            var dialog = new ContentDialog
            {
                Title = "Marks Slip — Select Assessments (max 2)",
                Content = stack,
                PrimaryButtonText = "OK — Build Slips",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
                Width = 520
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return null;

            return checkboxes.Where(c => c.IsChecked == true).Select(c => (string)c.Tag).ToList();
        }

        /// <summary>
        /// Lays 5 mid-term slips onto an A4-format page (794 × 1123 px), separated by
        /// dotted tear lines so each slip can be cut and handed out individually. Larger
        /// classes produce multiple sheets that overflow onto additional pages.
        /// </summary>
        private List<UIElement> BuildMarksSlipA4Pages(IReadOnlyList<MidTermSlipModel> slips)
        {
            const int SlipsPerPage = 5;
            const double PageWidth = 794;   // A4 portrait @ 96 DPI (210 mm)
            const double PageHeight = 1123; // A4 portrait @ 96 DPI (297 mm)
            const double SlipCellHeight = PageHeight / SlipsPerPage; // ~224.6 px per slip

            var pages = new List<UIElement>();
            int index = 0;
            while (index < slips.Count)
            {
                var grid = new Grid
                {
                    Width = PageWidth,
                    Height = PageHeight,
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
                };
                grid.RowDefinitions.Clear();
                for (int i = 0; i < SlipsPerPage; i++)
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(SlipCellHeight) });

                for (int row = 0; row < SlipsPerPage && index < slips.Count; row++, index++)
                {
                    var slip = new MidTermSlipView { DataContext = slips[index] };

                    // Scale the 794×270 slip into the ~249 px cell height.
                    var box = new Viewbox
                    {
                        Child = slip,
                        Stretch = Stretch.Uniform,
                        StretchDirection = StretchDirection.DownOnly
                    };
                    Grid.SetRow(box, row);
                    grid.Children.Add(box);

                    // Dotted tear line below every slip except the last row on the page.
                    if (row < SlipsPerPage - 1)
                    {
                        var tear = new Microsoft.UI.Xaml.Shapes.Rectangle
                        {
                            Height = 1,
                            Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                            StrokeDashArray = new DoubleCollection { 3, 3 },
                            StrokeThickness = 1,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Margin = new Thickness(12, 0, 12, 0)
                        };
                        Grid.SetRow(tear, row);
                        grid.Children.Add(tear);
                    }
                }
                pages.Add(grid);
            }
            return pages;
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
