using AutoTable.Converters;
using AutoTable.Models;
using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Printing;
using Microsoft.UI.Xaml;

namespace AutoTable.Views
{
    public sealed partial class FeeCollectionView : Page
    {
        public FeeCollectionViewModel ViewModel { get; } = new();
        public FeeCollectionView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private async void RecordPayment_Click(object sender, RoutedEventArgs e)
        {
            // Active-term enforcement: block if no term is active
            SimpleLookup? activeTerm = null;
            try { activeTerm = await AppServices.DataService!.GetActiveTermAsync(); } catch { }
            if (activeTerm == null)
            {
                var err = new ContentDialog
                {
                    Title = "No Active Term",
                    Content = "No academic term is currently active. Please create and activate a term under Term Management before recording payments.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await err.ShowAsync();
                return;
            }
            int? termId = activeTerm?.Id;
            var termLabel = activeTerm?.Name ?? "(no active term)";

            // Load active students once so search works without extra DB hits
            var students = new List<Student>();
            try
            {
                students = (await AppServices.DataService!.GetStudentsAsync()).Where(s => s.IsActive).ToList();
            }
            catch { }

            Student? selectedStudent = null;
            double? expected = null;
            bool suppressSearch = false; // stops repopulation when we echo the picked name into the box

            var dialog = new ContentDialog
            {
                Title = "Record Fee Payment",
                PrimaryButtonText = "Record",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var stack = new StackPanel { Spacing = 12, Width = 440 };

            var studentBox = new TextBox { Header = "Student", PlaceholderText = "Type a student name to search...", Width = 400 };
            // Shows Class + LIN beside the name field once a student is picked.
            var studentInfoBlock = new TextBlock
            {
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Bottom,
                TextWrapping = TextWrapping.Wrap,
                Visibility = Visibility.Collapsed,
                MaxWidth = 180,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
            };
            var amountBox = new TextBox { Header = "Amount", PlaceholderText = "Enter amount paid" };
            var hint = new TextBlock { FontSize = 12, Opacity = 1.0, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) };


            // Shared brushes for hint feedback — white text on dark dialog
            var MutedHintBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
            var ErrorHintBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x6B, 0x6B));

            // -- Inline results panel (non-blocking): rendered directly under the search
            //    field so the TextBox keeps keyboard focus and the list re-filters live
            //    on every keystroke instead of a light-dismiss Flyout stealing input. --
            var resultList = new ItemsControl();
            var resultScroll = new ScrollViewer
            {
                Content = resultList,
                MaxHeight = 220,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(0, 4, 0, 4)
            };
            var resultsHost = new Border
            {
                Child = resultScroll,
                CornerRadius = new CornerRadius(6),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x60, 0x60, 0x60)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x2A, 0x2A, 0x2A)),
                Visibility = Visibility.Collapsed
            };

            // Declared BEFORE the local functions below that capture them
            // (assigned once the "All classes"/"All streams" items are built).
            SimpleLookup? classFilterSelection = null;
            SimpleLookup? streamFilterSelection = null;

            void UpdateHint()
            {
                var className = selectedStudent?.ClassName ?? classFilterSelection?.Name ?? ViewModel.SelectedClass;
                hint.Text = $"Class: {(string.IsNullOrEmpty(className) ? "-" : className)}   |   Term: {termLabel}"
                          + (expected.HasValue ? $"   |   Expected: {expected.Value:N0}" : "");
                amountBox.Header = expected.HasValue ? $"Amount (expected: {expected.Value:N0})" : "Amount";
                hint.Foreground = MutedHintBrush;
            }

            async Task LoadExpectedForAsync(Student s)
            {
                expected = null;
                if (s.ClassId == null || termId == null) return;
                try
                {
                    var fees = await AppServices.DataService!.GetTermFeesAsync();
                    expected = fees.FirstOrDefault(tf => tf.ClassId == s.ClassId && tf.TermId == termId)?.Amount;
                }
                catch { }
            }


            async Task PopulateResultsAsync(string query)
            {
                resultList.Items.Clear();
                if (string.IsNullOrWhiteSpace(query)) { resultsHost.Visibility = Visibility.Collapsed; return; }

                // Lazy reload: if the initial roster load failed silently, retry once
                // so a transient DB hiccup can never leave the panel permanently blank.
                if (students.Count == 0)
                {
                    try
                    {
                        students = (await AppServices.DataService!.GetStudentsAsync()).Where(s => s.IsActive).ToList();
                    }
                    catch { }
                }

                // Match on the initials/prefix of ANY name part (first or second name),
                // with a general substring fallback for loose typing, plus LIN.
                bool MatchesName(Student s, string q)
                {
                    var trimmed = q.Trim();
                    if (trimmed.Length == 0) return true;

                    var tokens = (s.FullName ?? string.Empty)
                        .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Any(t => t.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase)))
                        return true;
                    if ((s.FullName ?? string.Empty).IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    if ((s.LIN ?? string.Empty).StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
                        return true;
                    return (s.LIN ?? string.Empty).IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                var matches = students.Where(s => MatchesName(s, query));
                if (classFilterSelection != null && classFilterSelection.Id != 0)
                    matches = matches.Where(s => s.ClassId == classFilterSelection.Id);
                if (streamFilterSelection != null && streamFilterSelection.Id != 0)
                    matches = matches.Where(s => s.StreamId == streamFilterSelection.Id);
                var matchesList = matches.Take(8).ToList();

                foreach (var s in matchesList)
                {
                    // Row layout: Name on the LEFT; Class - Stream over LIN on the RIGHT.
                    var row = new Grid { ColumnSpacing = 12 };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var nameBlock = new TextBlock
                    {
                        Text = s.FullName,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        FontSize = 14,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
                    };
                    Grid.SetColumn(nameBlock, 0);

                    var rightPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
                    rightPanel.Children.Add(new TextBlock
                    {
                        Text = $"{(string.IsNullOrEmpty(s.ClassName) ? "-" : s.ClassName)} - {(string.IsNullOrEmpty(s.StreamName) ? "-" : s.StreamName)}",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF))
                    });
                    rightPanel.Children.Add(new TextBlock
                    {
                        Text = $"LIN: {s.LIN}",
                        FontSize = 11,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF))
                    });
                    Grid.SetColumn(rightPanel, 1);

                    row.Children.Add(nameBlock);
                    row.Children.Add(rightPanel);

                    // Real Button host: guaranteed click semantics (a bare Border's
                    // Tapped proved unreliable inside the dialog's visual tree).
                    var rowHost = new Button
                    {
                        Content = row,
                        Padding = new Thickness(8, 6, 8, 6),
                        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                        BorderThickness = new Thickness(0),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Tag = s
                    };
                    rowHost.Click += (snd, e) =>
                    {
                        if ((snd as FrameworkElement)?.Tag is Student picked) _ = HandlePickAsync(picked);
                    };

                    resultList.Items.Add(rowHost);
                }
            }

            // Accepts "1000", "1,000", "50 000" etc. — locale-formatted input must not
            // silently fail TryParse and leave the Record button dead.
            static bool TryParseAmount(string? raw, out double value)
            {
                value = 0;
                var clean = (raw ?? string.Empty).Replace(",", string.Empty).Replace(" ", string.Empty);
                return double.TryParse(clean, out value) && value > 0;
            }

            // Shared entry point for search + filter changes: re-render results and
            // toggle visibility based on whether any rows were produced.
            async Task ApplySearchAsync()
            {
                await PopulateResultsAsync(studentBox.Text);
                resultsHost.Visibility = resultList.Items.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            // Live filter: results keep adjusting while the user continues typing.
            studentBox.TextChanged += async (s, e) =>
            {
                if (suppressSearch)
                {
                    suppressSearch = false;
                    return;
                }
                // Reset the name field width and hide info when user edits again.
                studentBox.Width = 400;
                studentInfoBlock.Visibility = Visibility.Collapsed;
                selectedStudent = null;
                expected = null;
                UpdateHint();
                await ApplySearchAsync();
            };

            async Task HandlePickAsync(Student picked)
            {
                selectedStudent = picked;
                suppressSearch = true; // do not re-open the popup when echoing the name
                studentBox.Text = picked.FullName;
                studentBox.SelectionStart = studentBox.Text.Length;

                // Shrink the name field and show Class + LIN beside it.
                studentBox.Width = 220;
                var classStream = string.IsNullOrEmpty(picked.StreamName)
                    ? (picked.ClassName ?? "-")
                    : $"{picked.ClassName} - {picked.StreamName}";
                studentInfoBlock.Text = $"{classStream}\nLIN: {picked.LIN}";
                studentInfoBlock.Visibility = Visibility.Visible;

                // Expected fee now reflects the STUDENT'S class under the active term.
                await LoadExpectedForAsync(picked);
                UpdateHint();
                resultsHost.Visibility = Visibility.Collapsed;
            }

            UpdateHint();

            // Class & stream filters to scale down the student search.
            var allClassesItem = new SimpleLookup { Id = 0, Name = "All classes" };
            var allStreamsItem = new SimpleLookup { Id = 0, Name = "All streams" };
            var classFilterItems = new List<object> { allClassesItem };
            var streamFilterItems = new List<object> { allStreamsItem };
            classFilterSelection = allClassesItem;
            streamFilterSelection = allStreamsItem;
            try
            {
                foreach (var c in await AppServices.DataService!.GetClassesAsync())
                    classFilterItems.Add(new SimpleLookup { Id = c.Id, Name = c.Name });
                foreach (var st in await AppServices.DataService!.GetAllStreamsAsync())
                    streamFilterItems.Add(new SimpleLookup { Id = st.Id, Name = st.Name });
            }
            catch { }

            var classFilter = new ComboBox
            {
                Header = "Class",
                Width = 200,
                ItemsSource = classFilterItems,
                DisplayMemberPath = nameof(SimpleLookup.Name),
                SelectedIndex = 0
            };
            var streamFilter = new ComboBox
            {
                Header = "Stream",
                Width = 200,
                ItemsSource = streamFilterItems,
                DisplayMemberPath = nameof(SimpleLookup.Name),
                SelectedIndex = 0
            };
            classFilter.SelectionChanged += async (_, _) =>
            {
                classFilterSelection = classFilter.SelectedItem as SimpleLookup;
                await ApplySearchAsync();
            };
            streamFilter.SelectionChanged += async (_, _) =>
            {
                streamFilterSelection = streamFilter.SelectedItem as SimpleLookup;
                await ApplySearchAsync();
            };

            var filterRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            // Default the modal's class filter to the page-selected class when possible
            try
            {
                if (!string.IsNullOrWhiteSpace(ViewModel.SelectedClass) && ViewModel.SelectedClass != "All")
                {
                    var match = classFilterItems.OfType<SimpleLookup>().FirstOrDefault(c => string.Equals(c.Name, ViewModel.SelectedClass, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        classFilter.SelectedItem = match;
                        classFilterSelection = match;
                    }
                }
            }
            catch { }

            filterRow.Children.Add(classFilter);
            filterRow.Children.Add(streamFilter);

            stack.Children.Add(filterRow);

            // Student row: name field + Class/LIN info beside it (infoBlock visible only after pick).
            var studentRow = new Grid { ColumnSpacing = 8 };
            studentRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            studentRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(studentBox, 0);
            Grid.SetColumn(studentInfoBlock, 1);
            studentRow.Children.Add(studentBox);
            studentRow.Children.Add(studentInfoBlock);
            stack.Children.Add(studentRow);

            stack.Children.Add(resultsHost);   // non-blocking inline results under the field
            stack.Children.Add(amountBox);
            stack.Children.Add(hint);
            dialog.Content = stack;

            // The Record button is ALWAYS enabled. Validation happens when the primary
            // button is clicked: an invalid submit cancels the close (args.Cancel is
            // reliable in PrimaryButtonClick, unlike Closing) and explains what is
            // missing inline — instead of silently closing the modal and recording nothing.
            dialog.PrimaryButtonClick += (s, args) =>
            {
                if (selectedStudent == null)
                {
                    args.Cancel = true;
                    hint.Text = "Pick a student from the list under the name field first.";
                    hint.Foreground = ErrorHintBrush;
                    hint.Opacity = 1;
                    return;
                }

                if (!TryParseAmount(amountBox.Text, out _))
                {
                    args.Cancel = true;
                    hint.Text = "Enter a valid amount greater than 0.";
                    hint.Foreground = ErrorHintBrush;
                    hint.Opacity = 1;
                }
            };

            var res = await dialog.ShowAsync();
            if (res == ContentDialogResult.Primary && selectedStudent != null
                && TryParseAmount(amountBox.Text, out var amt))
            {
                try
                {
                    await AppServices.DataService!.CreateFeePaymentAsync(
                        selectedStudent.Id, amt, termId, null, "Recorded via UI");
                    // Show the fresh payment in the table: point the term filter at the
                    // active term the payment was recorded under (when it is selectable),
                    // then reload so the row appears immediately after the modal closes.
                    if (activeTerm != null && ViewModel.Terms.Contains(activeTerm.Name))
                        ViewModel.SelectedTerm = activeTerm.Name;
                    await ViewModel.RefreshCommand!.ExecuteAsync(null);
                    ViewModel.StatusMessage = $"Recorded payment of {amt:N0} for {selectedStudent.FullName} ({termLabel}).";
                    // AppServices.Toasts.Show("Payment Recorded", $"Recorded payment of {amt:N0} for {selectedStudent.FullName} ({termLabel}).");
                }
                catch (Exception ex)
                {
                    ViewModel.StatusMessage = "Failed to record payment: " + ex.Message;
                    // Surface the failure loudly — a status line behind the closed modal is easy to miss.
                    var errDlg = new ContentDialog
                    {
                        Title = "Could not record payment",
                        Content = ex.Message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errDlg.ShowAsync();
                }
            }
        }

        // =====================================================================
        // Payment slip printing — a dedicated section for printing fee payment
        // slips per student (NOT the full report card). Uses the same WinUI
        // PrintManager pipeline as ReportCardsView.
        // =====================================================================

        private PrintDocument? _printDocument;
        private IPrintDocumentSource? _printDocumentSource;

        /// <summary>Holds the A4 sheets queued for printing — one slip per page.</summary>
        private readonly List<UIElement> _slipPages = new();

        private void RegisterSlipsForPrinting(IEnumerable<UIElement> pages)
        {
            UnregisterSlipsFromPrinting();
            _slipPages.AddRange(pages);
            _printDocument = new PrintDocument();
            _printDocumentSource = _printDocument.DocumentSource;
            _printDocument.Paginate += SlipDocument_Paginate;
            _printDocument.GetPreviewPage += SlipDocument_GetPreviewPage;
            _printDocument.AddPages += SlipDocument_AddPages;
            PrintManager.GetForCurrentView().PrintTaskRequested += SlipPrintTaskRequested;
        }

        private void UnregisterSlipsFromPrinting()
        {
            if (_printDocument != null)
            {
                _printDocument.Paginate -= SlipDocument_Paginate;
                _printDocument.GetPreviewPage -= SlipDocument_GetPreviewPage;
                _printDocument.AddPages -= SlipDocument_AddPages;
                _printDocument = null;
                _printDocumentSource = null;
            }
            try { PrintManager.GetForCurrentView().PrintTaskRequested -= SlipPrintTaskRequested; } catch { }
            _slipPages.Clear();
        }

        private void SlipPrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            var def = args.Request.GetDeferral();
            try
            {
                var printTask = args.Request.CreatePrintTask("Fee Payment Slip", requestArgs =>
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

        private void SlipDocument_Paginate(object? sender, PaginateEventArgs e)
        {
            if (_slipPages.Count > 0)
                _printDocument?.SetPreviewPageCount(_slipPages.Count, PreviewPageCountType.Final);
        }

        private void SlipDocument_GetPreviewPage(object? sender, GetPreviewPageEventArgs e)
        {
            if (e.PageNumber >= 1 && e.PageNumber <= _slipPages.Count)
                _printDocument?.SetPreviewPage(e.PageNumber, _slipPages[e.PageNumber - 1]);
        }

        private void SlipDocument_AddPages(object? sender, AddPagesEventArgs e)
        {
            foreach (var page in _slipPages)
                _printDocument?.AddPage(page);
            _printDocument?.AddPagesComplete();
        }

        /// <summary>
        /// Builds a single printable payment slip for a fee record.
        /// Deliberately a lightweight receipt — not the full report card.
        /// </summary>
        private static UIElement BuildSlipGrid(FeeRecord record)
        {
            static TextBlock MakeLabel(string text) => new()
            {
                Text = text,
                FontSize = 13,
                Opacity = 0.6
            };

            var details = new Grid { ColumnSpacing = 40, RowSpacing = 8 };
            details.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            details.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            void AddDetail(int col, int row, string label, string value)
            {
                var stack = new StackPanel { Spacing = 2 };
                stack.Children.Add(MakeLabel(label));
                stack.Children.Add(new TextBlock { Text = value, FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                Grid.SetColumn(stack, col);
                Grid.SetRow(stack, row);
                if (details.RowDefinitions.Count <= row)
                    details.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                details.Children.Add(stack);
            }

            AddDetail(0, 0, "Student", record.StudentName);
            AddDetail(1, 0, "LIN / Adm. No.", string.IsNullOrWhiteSpace(record.AdmissionNumber) ? "-" : record.AdmissionNumber);
            AddDetail(0, 1, "Class", record.ClassName);
            AddDetail(1, 1, "Term", string.IsNullOrWhiteSpace(record.Term) ? "-" : record.Term);

            var amounts = new Grid { ColumnSpacing = 24 };
            amounts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            amounts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            amounts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            void AddAmount(int col, string label, string value, Windows.UI.Color color)
            {
                var stack = new StackPanel { Spacing = 2 };
                stack.Children.Add(MakeLabel(label));
                stack.Children.Add(new TextBlock
                {
                    Text = value,
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new SolidColorBrush(color)
                });
                Grid.SetColumn(stack, col);
                amounts.Children.Add(stack);
            }

            AddAmount(0, "Expected", record.ExpectedAmount.ToString("N0"), Microsoft.UI.Colors.Black);
            AddAmount(1, "Paid", record.PaidAmount.ToString("N0"), Windows.UI.Color.FromArgb(0xFF, 0x0F, 0x7B, 0x0F));
            AddAmount(2, "Balance", record.Balance.ToString("N0"),
                record.Balance <= 0 ? Windows.UI.Color.FromArgb(0xFF, 0x0F, 0x7B, 0x0F) : Windows.UI.Color.FromArgb(0xFF, 0xC4, 0x2B, 0x1C));

            var footer = new StackPanel { Spacing = 2 };
            footer.Children.Add(MakeLabel($"Status: {record.PaymentStatus}"));
            footer.Children.Add(MakeLabel($"Last payment date: {record.PaymentDate}"));
            footer.Children.Add(MakeLabel($"Printed: {DateTime.Now:dd MMM yyyy HH:mm}"));
            footer.Children.Add(new TextBlock
            {
                Text = "_______________________",
                Margin = new Thickness(0, 32, 0, 0)
            });
            footer.Children.Add(MakeLabel("Authorised Signature"));

            var slip = new StackPanel { Spacing = 18, Padding = new Thickness(36), Background = new SolidColorBrush(Microsoft.UI.Colors.White) };
            slip.Children.Add(new TextBlock
            {
                Text = "AutoTable Academy",
                FontSize = 26,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            slip.Children.Add(new TextBlock
            {
                Text = "OFFICIAL FEE PAYMENT SLIP",
                FontSize = 14,
                Opacity = 0.7,
                CharacterSpacing = 120,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            slip.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 4, 0, 0)
            });
            slip.Children.Add(details);
            slip.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 4, 0, 0)
            });
            slip.Children.Add(amounts);
            slip.Children.Add(footer);

            return new Border
            {
                Child = slip,
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DarkGray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Microsoft.UI.Colors.White)
            };
        }

        private async Task ShowSlipPreviewAndPrintAsync(IReadOnlyList<UIElement> slips, string title)
        {
            if (slips.Count == 0) return;

            var dialog = new ContentDialog
            {
                Title = title,
                Content = new ScrollViewer
                {
                    Content = slips[0],
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                },
                PrimaryButtonText = "Print",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Width = 700,
                Height = 720
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                RegisterSlipsForPrinting(slips);
                // System print dialog: pick any installed printer or "Microsoft Print to PDF".
                if (!await PrintManager.ShowPrintUIAsync())
                {
                    await new ContentDialog
                    {
                        Title = "Printing unavailable",
                        Content = "Windows could not open the print dialog. Make sure at least one printer (or \"Microsoft Print to PDF\") is installed and enabled, then try again.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                }
            }
            finally { UnregisterSlipsFromPrinting(); }
        }

        private async void PrintSlip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: FeeRecord record })
            {
                var slips = new List<UIElement> { BuildSlipGrid(record) };
                await ShowSlipPreviewAndPrintAsync(slips, $"Payment Slip — {record.StudentName}");
            }
        }

        private async void PrintSlips_Click(object sender, RoutedEventArgs e)
        {
            var slips = ViewModel.FeeRecords.Select(BuildSlipGrid).ToList();
            await ShowSlipPreviewAndPrintAsync(slips, $"Payment Slips — {slips.Count} student(s)");
        }
    }
}
