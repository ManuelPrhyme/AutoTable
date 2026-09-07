using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class AuditLogView : Page
    {
        private readonly AuditLogViewModel _vm;
        private bool _optionsLoaded;
        private bool _suppressDateEvents;

        public AuditLogView()
        {
            InitializeComponent();
            _vm = new AuditLogViewModel();
            DataContext = _vm;
            Loaded += AuditLogView_Loaded;
        }

        private async void AuditLogView_Loaded(object sender, RoutedEventArgs e)
        {
            // Populate the filter dropdowns once; reload the table on every load.
            if (!_optionsLoaded)
            {
                _optionsLoaded = true;

                // Default both pickers to today's date (display only — the date
                // filter stays inactive until the user actively picks a date).
                _suppressDateEvents = true;
                FromPicker.Date = DateTimeOffset.Now;
                ToPicker.Date = DateTimeOffset.Now;
                _suppressDateEvents = false;

                await _vm.LoadFilterOptionsAsync();
            }
            await _vm.LoadAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.Filter = SearchBox.Text;
        }

        private void FromPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (_suppressDateEvents) return;
            _vm.StartDate = args.NewDate;
        }

        private void ToPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (_suppressDateEvents) return;
            _vm.EndDate = args.NewDate;
        }

        private async void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
        }
    }
}