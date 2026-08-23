using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
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
            // ── Scope selector ──
            var scopeBox = new ComboBox
            {
                Header = "Subject scope",
                Width = 280,
                ItemsSource = new[] { "Single Subject", "All Subjects in Class", "Specific Subjects" },
                SelectedIndex = 0
            };

            // ── Standard fields ──
            var nameBox = new TextBox { Header = "Assessment name", Width = 320 };
            var classPicker = new ComboBox { Header = "Class", Width = 240, DisplayMemberPath = "Name", SelectedIndex = -1 };
            var weightBox = new TextBox { Header = "Weight (%)", Width = 120, Text = "20" };
            var duePicker = new DatePicker { Header = "Due date", Date = DateTime.Today };
            var isClassWideBox = new CheckBox { Content = "Apply to entire class", IsChecked = true };
            var streamPicker = new ComboBox { Header = "Stream (when not class-wide)", Width = 240, DisplayMemberPath = "Name", SelectedIndex = -1, IsEnabled = false };

            // ── Subject pickers (shown/hidden based on scope) ──
            var singleSubjectPicker = new ComboBox { Header = "Subject", Width = 240, DisplayMemberPath = "Name", SelectedIndex = -1 };
            var multiSubjectPanel = new StackPanel { Spacing = 4, Visibility = Visibility.Collapsed };
            var multiSubjectHeader = new TextBlock { Text = "Select subjects", FontSize = 12, Margin = new Thickness(0, 0, 0, 2) };
            var multiSubjectScroll = new ScrollViewer { MaxHeight = 160, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var multiSubjectItems = new StackPanel { Spacing = 2 };
            multiSubjectScroll.Content = multiSubjectItems;
            multiSubjectPanel.Children.Add(multiSubjectHeader);
            multiSubjectPanel.Children.Add(multiSubjectScroll);

            var scopeHint = new TextBlock
            {
                Text = "One assessment per subject will be created.",
                FontSize = 11,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                Visibility = Visibility.Collapsed
            };

            // Track all subjects for the selected class (used by All/Specific scopes)
            var allClassSubjects = new List<AutoTable.Models.SimpleLookup>();

            // ── Load classes ──
            var classes = await AppServices.DataService!.GetClassesAsync();
            classPicker.ItemsSource = classes;
            if (classes.Count > 0) classPicker.SelectedIndex = 0;

            // ── When class changes, reload streams + subjects + multi-select checkboxes ──
            classPicker.SelectionChanged += async (s, ev) =>
            {
                streamPicker.ItemsSource = null;
                streamPicker.SelectedIndex = -1;
                singleSubjectPicker.ItemsSource = null;
                singleSubjectPicker.SelectedIndex = -1;
                allClassSubjects.Clear();
                multiSubjectItems.Children.Clear();

                if (classPicker.SelectedItem is AutoTable.Models.SimpleLookup cls)
                {
                    var streams = await AppServices.DataService!.GetStreamsForClassAsync(cls.Id);
                    streamPicker.ItemsSource = streams;
                    if (streams.Count > 0) streamPicker.SelectedIndex = 0;

                    allClassSubjects.AddRange(await AppServices.DataService!.GetSubjectsForClassAsync(cls.Id));
                    singleSubjectPicker.ItemsSource = allClassSubjects.Select(x => x.Name).ToList();
                    if (singleSubjectPicker.Items.Count > 0) singleSubjectPicker.SelectedIndex = 0;

                    // Build multi-select checkboxes
                    foreach (var subj in allClassSubjects)
                    {
                        multiSubjectItems.Children.Add(new CheckBox
                        {
                            Content = subj.Name,
                            Tag = subj.Id,
                            IsChecked = true,
                            FontSize = 13
                        });
                    }
                }
                else
                {
                    singleSubjectPicker.ItemsSource = null;
                }
            };

            // ── Scope change: show/hide appropriate subject controls ──
            void UpdateScopeVisuals()
            {
                var scope = scopeBox.SelectedIndex;
                bool isSingle = scope == 0;
                bool isAll = scope == 1;
                bool isSpecific = scope == 2;

                singleSubjectPicker.Visibility = isSingle ? Visibility.Visible : Visibility.Collapsed;
                multiSubjectPanel.Visibility = isSpecific ? Visibility.Visible : Visibility.Collapsed;
                scopeHint.Visibility = (isAll || isSpecific) ? Visibility.Visible : Visibility.Collapsed;
                scopeHint.Text = isAll
                    ? "One assessment per subject in this class will be created."
                    : "One assessment per selected subject will be created.";
            }

            scopeBox.SelectionChanged += (s, ev) => UpdateScopeVisuals();
            isClassWideBox.Checked += (s, ev) => streamPicker.IsEnabled = false;
            isClassWideBox.Unchecked += (s, ev) => streamPicker.IsEnabled = true;

            // Initial state
            UpdateScopeVisuals();

            // ── Assemble form ──
            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(nameBox);
            panel.Children.Add(classPicker);
            panel.Children.Add(scopeBox);
            panel.Children.Add(singleSubjectPicker);
            panel.Children.Add(multiSubjectPanel);
            panel.Children.Add(scopeHint);
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
                Width = 580
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var selectedClass = classPicker.SelectedItem as AutoTable.Models.SimpleLookup;
                    var selectedStream = streamPicker.SelectedItem as AutoTable.Models.SimpleLookup;
                    var scope = (AutoTable.Models.AssessmentScope)scopeBox.SelectedIndex;
                    var weight = int.TryParse(weightBox.Text, out var w) ? w : 0;
                    var isClassWide = isClassWideBox.IsChecked == true;

                    // Determine which subjects to create assessments for
                    List<string> subjectNames;
                    if (scope == AutoTable.Models.AssessmentScope.Single)
                    {
                        subjectNames = new List<string>
                        {
                            singleSubjectPicker.SelectedItem as string ?? string.Empty
                        };
                    }
                    else if (scope == AutoTable.Models.AssessmentScope.AllSubjects)
                    {
                        subjectNames = allClassSubjects.Select(s => s.Name).ToList();
                    }
                    else // SpecificSubjects
                    {
                        subjectNames = multiSubjectItems.Children
                            .OfType<CheckBox>()
                            .Where(cb => cb.IsChecked == true)
                            .Select(cb => cb.Content?.ToString() ?? "")
                            .Where(n => !string.IsNullOrWhiteSpace(n))
                            .ToList();
                    }

                    if (subjectNames.Count == 0 || string.IsNullOrWhiteSpace(subjectNames[0]))
                    {
                        var err = new ContentDialog { Title = "No subject selected", Content = "Please select at least one subject.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                        await err.ShowAsync();
                        return;
                    }

                    // Create one assessment per subject
                    var createdItems = new List<AutoTable.Models.AssessmentItem>();
                    foreach (var subjName in subjectNames)
                    {
                        var item = new AutoTable.Models.AssessmentItem
                        {
                            Name = nameBox.Text?.Trim() ?? string.Empty,
                            ClassName = selectedClass?.Name ?? string.Empty,
                            Subject = subjName,
                            Scope = scope,
                            WeightPercent = weight,
                            DueDate = duePicker.Date.DateTime,
                            IsClassWide = isClassWide,
                            StreamId = selectedStream?.Id,
                            StreamName = selectedStream?.Name
                        };
                        var created = await AppServices.DataService!.CreateAssessmentAsync(item);
                        if (created != null) createdItems.Add(created);
                    }

                    // Insert all created assessments at top of list
                    for (int i = createdItems.Count - 1; i >= 0; i--)
                    {
                        ViewModel.Assessments.Insert(0, createdItems[i]);
                    }
                    ViewModel.RefreshCounts();

                    ViewModel.StatusMessage = createdItems.Count == 1
                        ? $"Assessment '{createdItems[0].Name}' created for {createdItems[0].Subject}."
                        : $"Created {createdItems.Count} assessments for '{nameBox.Text?.Trim()}'.";
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
