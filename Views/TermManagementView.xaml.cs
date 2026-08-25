using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace AutoTable.Views
{
    public sealed partial class TermManagementView : Page
    {
        private readonly TermManagementViewModel _vm;
        public TermManagementView()
        {
            InitializeComponent();
            _vm = new TermManagementViewModel();
            DataContext = _vm;
            Loaded += TermManagementView_Loaded;
        }

        private async void TermManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
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

        private async void CreateTerm_Click(object sender, RoutedEventArgs e)
        {
            var name = NewTermName.Text?.Trim();
            var start = TermStart.Date;
            var end = TermEnd.Date;
            if (string.IsNullOrWhiteSpace(name)) return;

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
                NewTermName.Text = string.Empty;
                TermStart.Date = System.DateTime.Now;
                TermEnd.Date = System.DateTime.Now;
                var dlg = new ContentDialog { Title = "Term created", Content = $"Term '{name}' created successfully.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                await dlg.ShowAsync();
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
            if ((sender as Button)?.DataContext is AutoTable.Models.SimpleLookup cls)
            {
                // find FeeBox in visual tree - simple approach: look up ancestor Grid
                var grid = ((FrameworkElement)sender).Parent as FrameworkElement;
                // fallback: ask user to select a term first
                if (TermsList.SelectedItem is not AutoTable.Models.SimpleLookup term) return;

                // find FeeBox by name within the template
                var container = ClassesFeeList.ContainerFromItem(cls) as ListViewItem;
                if (container == null) return;
                var root = container.ContentTemplateRoot as FrameworkElement;
                var feeBox = root?.FindName("FeeBox") as TextBox;
                if (feeBox == null) return;
                if (double.TryParse(feeBox.Text, out var amt))
                {
                    try
                    {
                        await _vm.SetTermFeeAsync(term.Id, cls.Id, amt);
                        var dlg = new ContentDialog { Title = "Term fee set", Content = $"Fee {amt:C} set for class {cls.Name} in term {term.Name}.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                        await dlg.ShowAsync();
                    }
                    catch (System.Exception ex)
                    {
                        var dlg = new ContentDialog { Title = "Unable to set fee", Content = ex.Message, CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                        await dlg.ShowAsync();
                    }
                }
            }
        }
    }
}
