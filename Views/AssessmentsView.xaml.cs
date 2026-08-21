using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
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
            // Build a simple form for creating a new assessment
            var nameBox = new TextBox { Header = "Assessment name", Width = 320 };
            var classPicker = new ComboBox { Header = "Class", Width = 200, ItemsSource = ViewModel.Classes, SelectedIndex = 0 };
            var subjectPicker = new ComboBox { Header = "Subject", Width = 200, ItemsSource = ViewModel.Subjects, SelectedIndex = 0 };
            var weightBox = new TextBox { Header = "Weight (%)", Width = 120, Text = "20" };
            var duePicker = new DatePicker { Header = "Due date", Date = DateTime.Today };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(nameBox);
            panel.Children.Add(classPicker);
            panel.Children.Add(subjectPicker);
            panel.Children.Add(weightBox);
            panel.Children.Add(duePicker);

            var dialog = new ContentDialog
            {
                Title = "Create Assessment",
                Content = panel,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
                Width = 520
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // attempt to create the assessment via data service
                try
                {
                    var item = new AssessmentItem
                    {
                        Name = nameBox.Text?.Trim() ?? string.Empty,
                        ClassName = classPicker.SelectedItem as string ?? string.Empty,
                        Subject = subjectPicker.SelectedItem as string ?? string.Empty,
                        WeightPercent = int.TryParse(weightBox.Text, out var w) ? w : 0,
                        DueDate = duePicker.Date.DateTime
                    };
                    await AppServices.DataService.CreateAssessmentAsync(item);
                    await ViewModel.RefreshFilterCommand.ExecuteAsync(null);
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
