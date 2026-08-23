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
                Width = 300,
                ItemsSource = new[]
                {
                    "Single Subject",
                    "All Subjects in Class",
                    "Specific Subjects (custom)",
                    "All Subjects in School (general exam)"
                },
                SelectedIndex = 0
            };

            // ── Standard fields ──
            var nameBox = new TextBox { Header = "Assessment name", Width = 320 };
            var classPicker = new ComboBox { Header = "Class", Width = 240, DisplayMemberPath = "Name", SelectedIndex = -1 };
            var weightBox = new TextBox { Header = "Weight (%)", Width = 120, Text = "20" };
            var duePicker = new DatePicker { Header = "Due date", Date = DateTime.Today };

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
                FontSize = 11,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                Visibility = Visibility.Collapsed
            };

            // Track subjects for the selected class
            var allClassSubjects = new List<AutoTable.Models.SimpleLookup>();
            // Track ALL subjects across all classes (for AllInSchool scope)
            var allSchoolSubjects = new List<string>();

            // ── Load classes ──
            var classes = await AppServices.DataService!.GetClassesAsync();
            classPicker.ItemsSource = classes;
            if (classes.Count > 0) classPicker.SelectedIndex = 0;

            // ── When class changes, reload streams + subjects ──
            classPicker.SelectionChanged += async (s, ev) =>
            {
                singleSubjectPicker.ItemsSource = null;
                singleSubjectPicker.SelectedIndex = -1;
                allClassSubjects.Clear();
                multiSubjectItems.Children.Clear();

                if (classPicker.SelectedItem is AutoTable.Models.SimpleLookup cls)
                {
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

            // Preload all school subjects
            allSchoolSubjects.AddRange(allClassSubjects.Select(s => s.Name));

            // ── Scope change: show/hide appropriate controls ──
            void UpdateScopeVisuals()
            {
                var scope = scopeBox.SelectedIndex;
                bool isSingle = scope == 0;
                bool isAllInClass = scope == 1;
                bool isSpecific = scope == 2;
                bool isAllInSchool = scope == 3;

                // Show/hide class picker (hidden for school-wide)
                classPicker.Visibility = isAllInSchool ? Visibility.Collapsed : Visibility.Visible;

                // Show/hide subject controls
                singleSubjectPicker.Visibility = isSingle ? Visibility.Visible : Visibility.Collapsed;
                multiSubjectPanel.Visibility = isSpecific ? Visibility.Visible : Visibility.Collapsed;

                // Hint text
                scopeHint.Visibility = Visibility.Visible;
                if (isAllInClass)
                {
                    var clsName = (classPicker.SelectedItem as AutoTable.Models.SimpleLookup)?.Name ?? "the class";
                    scopeHint.Text = $"Creates one assessment per subject in {clsName}.";
                }
                else if (isSpecific)
                    scopeHint.Text = "Check the subjects below. One assessment per selected subject will be created.";
                else if (isAllInSchool)
                    scopeHint.Text = "Creates one assessment per subject across ALL classes in the school.";
                else
                    scopeHint.Visibility = Visibility.Collapsed;
            }

            scopeBox.SelectionChanged += (s, ev) => UpdateScopeVisuals();

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
                    var scope = (AutoTable.Models.AssessmentScope)scopeBox.SelectedIndex;
                    var weight = int.TryParse(weightBox.Text, out var w) ? w : 0;
                    var assessmentName = nameBox.Text?.Trim() ?? string.Empty;
                    var dueDate = duePicker.Date.DateTime;

                    if (string.IsNullOrWhiteSpace(assessmentName))
                    {
                        var err = new ContentDialog { Title = "Name required", Content = "Please enter an assessment name.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                        await err.ShowAsync();
                        return;
                    }

                    // ── Resolve subjects to create assessments for ──
                    var createdItems = new List<AutoTable.Models.AssessmentItem>();

                    if (scope == AutoTable.Models.AssessmentScope.Single)
                    {
                        var subjName = singleSubjectPicker.SelectedItem as string ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(subjName))
                        {
                            var err = new ContentDialog { Title = "No subject selected", Content = "Please select a subject.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                        if (selectedClass == null)
                        {
                            var err = new ContentDialog { Title = "No class selected", Content = "Please select a class.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                        var item = BuildAssessmentItem(assessmentName, selectedClass.Name, subjName, scope, weight, dueDate, null);
                        var created = await AppServices.DataService!.CreateAssessmentAsync(item);
                        if (created != null) createdItems.Add(created);
                    }
                    else if (scope == AutoTable.Models.AssessmentScope.AllInClass)
                    {
                        if (selectedClass == null)
                        {
                            var err = new ContentDialog { Title = "No class selected", Content = "Please select a class.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                        // Reload subjects for the class to be sure
                        var subjects = (await AppServices.DataService!.GetSubjectsForClassAsync(selectedClass.Id)).Select(s => s.Name).ToList();
                        if (subjects.Count == 0)
                        {
                            var err = new ContentDialog { Title = "No subjects", Content = $"No subjects assigned to {selectedClass.Name}. Add subjects under Classes Management first.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                        foreach (var subjName in subjects)
                        {
                            var item = BuildAssessmentItem(assessmentName, selectedClass.Name, subjName, scope, weight, dueDate, null);
                            var created = await AppServices.DataService!.CreateAssessmentAsync(item);
                            if (created != null) createdItems.Add(created);
                        }
                    }
                    else if (scope == AutoTable.Models.AssessmentScope.SpecificSubjects)
                    {
                        if (selectedClass == null)
                        {
                            var err = new ContentDialog { Title = "No class selected", Content = "Please select a class.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                        var selectedSubjects = multiSubjectItems.Children
                            .OfType<CheckBox>()
                            .Where(cb => cb.IsChecked == true)
                            .Select(cb => cb.Content?.ToString() ?? "")
                            .Where(n => !string.IsNullOrWhiteSpace(n))
                            .ToList();
                        if (selectedSubjects.Count == 0)
                        {
                            var err = new ContentDialog { Title = "No subjects selected", Content = "Please check at least one subject.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                        foreach (var subjName in selectedSubjects)
                        {
                            var item = BuildAssessmentItem(assessmentName, selectedClass.Name, subjName, scope, weight, dueDate, null);
                            var created = await AppServices.DataService!.CreateAssessmentAsync(item);
                            if (created != null) createdItems.Add(created);
                        }
                    }
                    else if (scope == AutoTable.Models.AssessmentScope.AllInSchool)
                    {
                        // Create assessments for ALL classes × ALL subjects
                        var allClasses = await AppServices.DataService!.GetClassesAsync();
                        foreach (var cls in allClasses)
                        {
                            var subjects = (await AppServices.DataService!.GetSubjectsForClassAsync(cls.Id)).Select(s => s.Name).ToList();
                            foreach (var subjName in subjects)
                            {
                                var item = BuildAssessmentItem(assessmentName, cls.Name, subjName, scope, weight, dueDate, null);
                                var created = await AppServices.DataService!.CreateAssessmentAsync(item);
                                if (created != null) createdItems.Add(created);
                            }
                        }
                    }

                    // Insert all created assessments at top of list
                    for (int i = createdItems.Count - 1; i >= 0; i--)
                    {
                        ViewModel.Assessments.Insert(0, createdItems[i]);
                    }
                    ViewModel.RefreshCounts();

                    ViewModel.StatusMessage = createdItems.Count == 1
                        ? $"Assessment '{createdItems[0].Name}' created for {createdItems[0].Subject}."
                        : $"Created {createdItems.Count} assessments for '{assessmentName}'.";
                }
                catch (Exception ex)
                {
                    var err = new ContentDialog { Title = "Unable to create assessment", Content = ex.Message, CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                    await err.ShowAsync();
                }
            }
        }

        private static AssessmentItem BuildAssessmentItem(
            string name, string className, string subject, AssessmentScope scope,
            int weight, DateTime dueDate, int? streamId)
        {
            return new AssessmentItem
            {
                Name = name,
                ClassName = className,
                Subject = subject,
                Scope = scope,
                WeightPercent = weight,
                DueDate = dueDate,
                IsClassWide = true,
                StreamId = streamId
            };
        }
    }
}
