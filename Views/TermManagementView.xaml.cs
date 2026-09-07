using AutoTable.ViewModels;
using AutoTable.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Linq;
using System.Text.Json;

namespace AutoTable.Views
{
    public sealed partial class TermManagementView : Page
    {
        private readonly TermManagementViewModel _vm;
        // ── ROLE-BASED GATING (dormant during development) ──────
        // Uncomment _isAdmin and the guards below when enforcing role restrictions.
        // private readonly bool _isAdmin;

        public TermManagementView()
        {
            InitializeComponent();
            _vm = new TermManagementViewModel();
            DataContext = _vm;
            // _isAdmin = SessionService.Instance.IsAdministrator;
            Loaded += TermManagementView_Loaded;
        }

        private async void TermManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            // ── ROLE-BASED GATING (dormant during development) ──────
            // Hide Create Term section for non-admins:
            // if (!_isAdmin) CreateTermCard.Visibility = Visibility.Collapsed;

            await _vm.LoadAsync();

            // Wire up term selection to update the fee panel context
            TermsList.SelectionChanged += TermsList_SelectionChanged;

            // Update active indicators after terms are loaded
            UpdateActiveIndicators();

            // First-launch setup: auto-open the create-term modal once when
            // the class-creation flow finished and queued this step.
            if (SessionService.Instance.ShouldAutoOpenTermCreation)
            {
                SessionService.Instance.ShouldAutoOpenTermCreation = false;
                await OpenCreateTermModalAsync();
            }

            // Write quick diagnostic snapshot of loaded terms to temp for troubleshooting
            try
            {
                var list = new System.Text.StringBuilder();
                foreach (var t in _vm.Terms)
                {
                    list.AppendLine($"{t.Id}: {t.Name}");
                }
                var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "autotable_terms_snapshot.txt");
                System.IO.File.WriteAllText(path, $"LoadedTerms:\r\n{list}\r\nFlags: DEMO_MODE={Environment.GetEnvironmentVariable("AUTOTABLE_DEMO_MODE")}, EPHEMERAL={Environment.GetEnvironmentVariable("AUTOTABLE_DEV_EPHEMERAL_DB")}");
            }
            catch { }
        }

        private async void TermsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TermsList.SelectedItem is AutoTable.Models.SimpleLookup selectedTerm)
            {
                SelectedTermLabel.Text = $"Fees for: {selectedTerm.Name} — enter amounts below, then click Set.";

                // Show KPI cards and refresh school-wide totals for this term
                KpiHeader.Text = $"School totals for {selectedTerm.Name}";
                KpiCards.Visibility = Visibility.Visible;
                await _vm.RefreshSchoolKpisAsync(selectedTerm.Id, selectedTerm.Name);

                // Pre-fill FeeBox values with existing fees for this term
                foreach (var item in ClassesFeeList.Items)
                {
                    if (item is AutoTable.Models.SimpleLookup cls)
                    {
                        var container = ClassesFeeList.ContainerFromItem(cls) as ListViewItem;
                        var root = container?.ContentTemplateRoot as FrameworkElement;
                        var feeBox = root?.FindName("FeeBox") as TextBox;
                        if (feeBox != null)
                        {
                            var existing = _vm.GetFeeForClassInTerm(cls.Id, selectedTerm.Id);
                            feeBox.Text = existing > 0 ? existing.ToString("N0") : string.Empty;
                        }
                    }
                }
            }
            else
            {
                SelectedTermLabel.Text = "Select a term from the list to set fees.";
                KpiHeader.Text = "Select a term to view school-wide totals.";
                KpiCards.Visibility = Visibility.Collapsed;
            }
        }

        private async void CreateTerm_Click(object sender, RoutedEventArgs e)
        {
            // ── ROLE-BASED GATING (dormant during development) ──────
            // if (!_isAdmin)
            // {
            //     var err = new ContentDialog { Title = "Access Restricted", Content = "Only administrators can create terms.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
            //     await err.ShowAsync();
            //     return;
            // }
            await OpenCreateTermModalAsync();
        }

        /// <summary>
        /// Opens the create-term modal (name + start/end dates). Used both by
        /// the Create Term button on this page and by the first-launch setup
        /// flow (auto-opened via ShouldAutoOpenTermCreation).
        /// </summary>
        private async Task OpenCreateTermModalAsync()
        {
            // ── ROLE-BASED GATING (dormant during development) ──────
            // if (!_isAdmin)
            // {
            //     var err = new ContentDialog { Title = "Access Restricted", Content = "Only administrators can create terms.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
            //     await err.ShowAsync();
            //     return;
            // }
            var nameBox = new TextBox { Header = "Term name", PlaceholderText = "e.g. Term 1, 2026", Width = 300 };
            var startPicker = new CalendarDatePicker
            {
                Header = "Start date",
                PlaceholderText = "Pick start date",
                Description = "Tap to open the calendar",
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var endPicker = new CalendarDatePicker
            {
                Header = "End date",
                PlaceholderText = "Pick end date",
                Description = "Tap to open the calendar",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var dialog = new ContentDialog
            {
                Title = "New Term",
                Content = new StackPanel { Spacing = 4, Children = { nameBox, startPicker, endPicker } },
                PrimaryButtonText = "Create Term",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            var name = nameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                var missing = new ContentDialog { Title = "Term name required.", Content = "Enter a name for the term.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                await missing.ShowAsync();
                return;
            }

            if (startPicker.Date == null || endPicker.Date == null)
            {
                var err = new ContentDialog { Title = "Dates required", Content = "Please select both start and end dates for the term.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                await err.ShowAsync();
                return;
            }
            var start = startPicker.Date.Value;
            var end = endPicker.Date.Value;

            // ── Unusual-timing warning ──────────────────────────────────────────
            // Expected creation windows (by current calendar month):
            //   Term 1: January-April   (usually February or earlier, through April)
            //   Term 2: April-July      (late April, e.g. last week, through July)
            //   Term 3: September-November
            var termNo = ExtractTermNumber(name);
            if (termNo.HasValue && IsOutsideExpectedWindow(termNo.Value, DateTime.Now.Month))
            {
                var window = DescribeWindow(termNo.Value);
                var confirm = new ContentDialog
                {
                    Title = "Unusual creation date",
                    Content = $"It is currently {DateTime.Now:MMMM}. {window}.\r\n\r\n" +
                              $"Are you sure you want to continue creating '{name}' now?",
                    PrimaryButtonText = "Yes, create it",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var choice = await confirm.ShowAsync();
                if (choice != ContentDialogResult.Primary) return;
            }

            try
            {
                await _vm.CreateTermAsync(name, start.DateTime, end.DateTime);

                var dlg = new ContentDialog { Title = "Term created", Content = $"Term '{name}' created successfully.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                await dlg.ShowAsync();
                UpdateActiveIndicators();

                // Toast
                AppServices.Toasts.Show("Term Created", $"Term '{name}' created successfully.");

                // Audit log
                await AppServices.Audit.LogAsync("Academic", "Create", "Term", null, name,
                    JsonSerializer.Serialize(new { start, end }), isSuccess: true);
            }
            catch (System.Exception ex)
            {
                var dlg = new ContentDialog { Title = "Unable to create term", Content = ex.Message, CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                await dlg.ShowAsync();
            }
        }

        /// <summary>Extracts the first 1-digit term number from a name like "Term 2, 2026" or "TERM3".</summary>
        private static int? ExtractTermNumber(string name)
        {
            // Look for a digit 1-3 attached to the word "term" so years ("2026") never match.
            var m = System.Text.RegularExpressions.Regex.Match(name, @"term\D{0,3}([1-3])",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value[0] - '0';
            return null; // no recognisable term number - skip the timing check
        }

        private static bool IsOutsideExpectedWindow(int termNumber, int month) => termNumber switch
        {
            1 => month < 1 || month > 4,   // Jan-Apr
            2 => month < 4 || month > 7,   // late Apr-Jul
            3 => month < 9 || month > 11,  // Sep-Nov
            _ => false
        };

        private static string DescribeWindow(int termNumber) => termNumber switch
        {
            1 => "Term 1 usually runs between February (or earlier) and April",
            2 => "Term 2 usually runs between late April and July",
            3 => "Term 3 usually runs between September and November",
            _ => string.Empty
        };

        private async void SetFee_Click(object sender, RoutedEventArgs e)
        {
            // ── ROLE-BASED GATING (dormant during development) ──────
            // if (!_isAdmin)
            // {
            //     var err = new ContentDialog { Title = "Access Restricted", Content = "Only administrators can set term fees.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
            //     await err.ShowAsync();
            //     return;
            // }
            if ((sender as Button)?.DataContext is AutoTable.Models.SimpleLookup cls)
            {
                // Must select a term first
                if (TermsList.SelectedItem is not AutoTable.Models.SimpleLookup term)
                {
                    _vm.StatusMessage = "Select a term from the list before setting a fee.";
                    return;
                }

                // Find FeeBox in the visual tree via the ListView container
                var container = ClassesFeeList.ContainerFromItem(cls) as ListViewItem;
                if (container == null) return;
                var root = container.ContentTemplateRoot as FrameworkElement;
                var feeBox = root?.FindName("FeeBox") as TextBox;
                if (feeBox == null) return;
                if (double.TryParse(feeBox.Text, out var amt) && amt > 0)
                {
                    try
                    {
                        await _vm.SetTermFeeAsync(term.Id, cls.Id, amt);
                        _vm.StatusMessage = $"Fee {amt:N0} set for {cls.Name} in {term.Name}.";

                        // Toast
                        AppServices.Toasts.Show("Fee Set", $"Fee {amt:N0} UGX set for {cls.Name} in {term.Name}.");

                        // Audit log
                        await AppServices.Audit.LogAsync("Financial", "Set", "TermFee", null,
                            $"{cls.Name} - {term.Name}",
                            JsonSerializer.Serialize(new { amount = amt, classId = cls.Id, termId = term.Id }),
                            isSuccess: true);

                        // Refresh school-wide KPIs since expected amounts changed
                        await _vm.RefreshSchoolKpisAsync(term.Id, term.Name);
                    }
                    catch (System.Exception ex)
                    {
                        var dlg = new ContentDialog { Title = "Unable to set fee", Content = ex.Message, CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                        await dlg.ShowAsync();

                        // Audit log failure
                        await AppServices.Audit.LogAsync("Financial", "Set", "TermFee", null,
                            $"{cls.Name} - {term.Name}", null, isSuccess: false, errorMessage: ex.Message);
                    }
                }
                else
                {
                    _vm.StatusMessage = "Enter a valid fee amount greater than 0.";
                }
            }
        }

        private async void ActivateTerm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int termId)
            {
                await _vm.ActivateTermAsync(termId);
                UpdateActiveIndicators();
            }
        }

        private async void DeactivateTerm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int termId)
            {
                await _vm.DeactivateTermAsync(termId);
                UpdateActiveIndicators();
            }
        }

        /// <summary>Walks the visual tree of TermsList and updates indicator dot + button visibility for each term.</summary>
        private void UpdateActiveIndicators()
        {
            var activeId = _vm.ActiveTermId;
            var now = DateTime.UtcNow;
            foreach (var item in TermsList.Items)
            {
                if (item is AutoTable.Models.SimpleLookup term)
                {
                    var container = TermsList.ContainerFromItem(term) as ListViewItem;
                    if (container == null) continue;
                    var root = container.ContentTemplateRoot as FrameworkElement;
                    if (root == null) continue;

                    // Active indicator dot: green for active, muted otherwise
                    var indicator = root.FindName("ActiveIndicator") as Border;
                    if (indicator != null)
                    {
                        indicator.Background = (term.Id == activeId)
                            ? (Brush)Application.Current.Resources["SuccessGreenBrush"]
                            : (Brush)Application.Current.Resources["TextMutedBrush"];
                    }

                    // Button visibility:
                    //   - Active term: hide "Set Active", show "Deactivate"
                    //   - Inactive & not ended: show "Set Active", hide "Deactivate"
                    //   - Ended term: hide both
                    var activateBtn = root.FindName("ActivateBtn") as Button;
                    var deactivateBtn = root.FindName("DeactivateBtn") as Button;
                    bool isActive = term.Id == activeId;
                    bool hasEnded = term.EndDate.HasValue && term.EndDate.Value < now;

                    if (activateBtn != null)
                        activateBtn.Visibility = (!isActive && !hasEnded) ? Visibility.Visible : Visibility.Collapsed;
                    if (deactivateBtn != null)
                        deactivateBtn.Visibility = (isActive && !hasEnded) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
    }
}
