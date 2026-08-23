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
