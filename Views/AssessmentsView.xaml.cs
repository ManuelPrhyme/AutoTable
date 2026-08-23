using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using AutoTable.Models;
using System.Threading.Tasks;

namespace AutoTable.Views
{
    public sealed partial class AssessmentsView : Page
    {
        public AssessmentsViewModel ViewModel { get; } = new();
        public AssessmentsView()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private async void NewAssessment_Click(object sender, RoutedEventArgs e)
        {
            // Build a form that supports class-wide or stream-specific assessments
            var nameBox = new TextBox { Header = "Assessment name", Width = 320 };

            // class picker uses SimpleLookup items from the data service so we can get the Id
            var classPicker = new ComboBox { Header = "Class", Width = 240, DisplayMemberPath = "Name", SelectedIndex = -1 };
            var subjectPicker = new ComboBox { Header = "Subject", Width = 240, DisplayMemberPath = "Name", SelectedIndex = -1 };
            var weightBox = new TextBox { Header = "Weight (%)", Width = 120, Text = "20" };
            var duePicker = new DatePicker { Header = "Due date", Date = DateTime.Today };

            var isClassWideBox = new CheckBox { Content = "Apply to entire class", IsChecked = true };
            var streamPicker = new ComboBox { Header = "Stream (when not class-wide)", Width = 240, DisplayMemberPath = "Name", SelectedIndex = -1, IsEnabled = false };

            // load classes into classPicker
            var classes = await AppServices.DataService!.GetClassesAsync();
            classPicker.ItemsSource = classes;
            if (classes.Count > 0) classPicker.SelectedIndex = 0;

            // when class changes, load streams AND subjects assigned to that class
            classPicker.SelectionChanged += async (s, ev) =>
            {
                streamPicker.ItemsSource = null;
                streamPicker.SelectedIndex = -1;
                subjectPicker.ItemsSource = null;
                subjectPicker.SelectedIndex = -1;
                if (classPicker.SelectedItem is AutoTable.Models.SimpleLookup cls)
                {
                    var streams = await AppServices.DataService!.GetStreamsForClassAsync(cls.Id);
                    streamPicker.ItemsSource = streams;
                    if (streams.Count > 0) streamPicker.SelectedIndex = 0;

                    var subjectsForClass = await AppServices.DataService!.GetSubjectsForClassAsync(cls.Id);
                    subjectPicker.ItemsSource = subjectsForClass.Select(x => x.Name).ToList();
                    if (subjectPicker.Items.Count > 0) subjectPicker.SelectedIndex = 0;
                }
                else
                {
                    // no class selected: show empty subject list
                    subjectPicker.ItemsSource = null;
                }
            };

            isClassWideBox.Checked += (s, ev) => streamPicker.IsEnabled = false;
            isClassWideBox.Unchecked += (s, ev) => streamPicker.IsEnabled = true;

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(nameBox);
            panel.Children.Add(classPicker);
            panel.Children.Add(subjectPicker);
            panel.Children.Add(weightBox);
            panel.Children.Add(duePicker);
            panel.Children.Add(isClassWideBox);
            panel.Children.Add(streamPicker);

            var dialog = new ContentDialog
            {
                Title = "Create Assessment",
                Content = panel,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
                Width = 560
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var selectedClass = classPicker.SelectedItem as AutoTable.Models.SimpleLookup;
                    var selectedSubject = subjectPicker.SelectedItem as string ?? string.Empty;
                    var selectedStream = streamPicker.SelectedItem as AutoTable.Models.SimpleLookup;
                    var item = new AssessmentItem
                    {
                        Name = nameBox.Text?.Trim() ?? string.Empty,
                        ClassName = selectedClass?.Name ?? string.Empty,
                        Subject = selectedSubject,
                        WeightPercent = int.TryParse(weightBox.Text, out var w) ? w : 0,
                        DueDate = duePicker.Date.DateTime,
                        IsClassWide = isClassWideBox.IsChecked == true,
                        StreamId = selectedStream?.Id,
                        StreamName = selectedStream?.Name
                    };

                    var created = await AppServices.DataService!.CreateAssessmentAsync(item);
                    // Insert created assessment at top of list in the view model if present
                    if (created != null)
                    {
                        ViewModel.Assessments.Insert(0, created);
                        ViewModel.RefreshCounts();
                    }
                }
                catch (Exception ex)
                {
                    var err = new ContentDialog { Title = "Unable to create assessment", Content = ex.Message, CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                    await err.ShowAsync();
                }
            }
        }
    }
}
