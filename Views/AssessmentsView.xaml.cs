using AutoTable.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
            // â”€â”€ Active-term enforcement: block if no term is active â”€â”€
            var activeTerm = await AppServices.DataService!.GetActiveTermAsync();
            if (activeTerm == null)
            {
                var err = new ContentDialog
                {
                    Title = "No Active Term",
                    Content = "No academic term is currently active. Please create and activate a term under Term Management before creating assessments.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await err.ShowAsync();
                return;
            }

// â”€â”€ Top-level scope choice: Entire School / Class / Stream â”€â”€
            var scopePanel = new StackPanel { Spacing = 6 };
            scopePanel.Children.Add(new TextBlock
            {
                Text = "Assessment scope",
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            var entireSchoolCheck = new CheckBox { Content = "Entire School", FontSize = 13 };
            var classCheck = new CheckBox { Content = "Class", FontSize = 13 };
            var streamCheck = new CheckBox { Content = "Stream", FontSize = 13 };
            var scopeChecks = new[] { entireSchoolCheck, classCheck, streamCheck };
            foreach (var sck in scopeChecks) scopePanel.Children.Add(sck);

            // Secondary subject granularity (single / all / specific) shown for Class or Stream.
            var subjectScopeBox = new ComboBox
            {
                Header = "Subjects",
                Width = 300,
                ItemsSource = new[] { "Single Subject", "All Subjects in Class", "Specific Subjects (custom)" },
                SelectedIndex = 0
            };

            // â”€â”€ Standard fields â”€â”€
            var nameBox = new TextBox { Header = "Assessment name", Width = 320 };
            var classPicker = new ComboBox { Header = "Class", Width = 240, DisplayMemberPath = "Name", SelectedIndex = -1 };
            var weightBox = new TextBox { Header = "Weight (%)", Width = 120, Text = "20", TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left };
            // Calendar-style picker: a month grid with day cells and next/previous month
            // navigation (clicking the header switches to year/decade views).
            var duePicker = new CalendarDatePicker
            {
                Header = "Due date",
                Date = DateTime.Today,
                PlaceholderText = "Pick a date",
                Description = "Tap to open the calendar",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // â”€â”€ Promotion role selector â”€â”€
            var promoRoleBox = new ComboBox
            {
                Header = "Promotion role",
                Width = 300,
                ItemsSource = new[]
                {
                    "Just an assessment",
                    "Contributory (End of Year / Promotional)",
                    "End of Year (Promotional)",
                    "End of Term",
                    "Contributory (End of Term)"
                },
                SelectedIndex = 0
            };
var promoRoleHint = new TextBlock
            {
                FontSize = 11,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                Text = "End-of-Year / End-of-Term papers and their contributors appear on the report card. End of Year (Promotional) is the deciding exam; 'Just an assessment' is excluded from report cards."
            };

            // â”€â”€ Expandable details panel (hidden until a scope is chosen) â”€â”€
            var detailsPanel = new StackPanel { Spacing = 10, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 6, 0, 0) };
            var classStreamRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            // â”€â”€ Subject pickers â”€â”€
            var singleSubjectPicker = new ComboBox { Header = "Subject", Width = 240, SelectedIndex = -1 };
            var multiSubjectPanel = new StackPanel { Spacing = 4, Visibility = Visibility.Collapsed };
            var multiSubjectHeader = new TextBlock { Text = "Select subjects", FontSize = 12, Margin = new Thickness(0, 0, 0, 2) };
            var multiSubjectScroll = new ScrollViewer { MaxHeight = 160, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var multiSubjectItems = new StackPanel { Spacing = 2 };
            multiSubjectScroll.Content = multiSubjectItems;
            multiSubjectPanel.Children.Add(multiSubjectHeader);
            multiSubjectPanel.Children.Add(multiSubjectScroll);

            // ── Stream multi-select (Stream scope: a paper can target several streams) ──
            var streamMultiPanel = new StackPanel { Spacing = 4, Visibility = Visibility.Collapsed };
            var streamMultiHeader = new TextBlock { Text = "Select streams", FontSize = 12, Margin = new Thickness(0, 0, 0, 2) };
            var streamMultiScroll = new ScrollViewer { MaxHeight = 160, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var streamItems = new StackPanel { Spacing = 2 };
            streamMultiScroll.Content = streamItems;
            streamMultiPanel.Children.Add(streamMultiHeader);
            streamMultiPanel.Children.Add(streamMultiScroll);

            var scopeHint = new TextBlock
            {
                FontSize = 11,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                Visibility = Visibility.Collapsed
            };

            var allClassSubjects = new List<AutoTable.Models.SimpleLookup>();
            var allSchoolSubjects = new List<string>();

            // â”€â”€ Load classes â”€â”€
            var classes = await AppServices.DataService!.GetClassesAsync();
            classPicker.ItemsSource = classes;

            // â”€â”€ When class changes, reload streams + subjects â”€â”€
            classPicker.SelectionChanged += async (s, ev) =>
            {
                singleSubjectPicker.ItemsSource = null;
                singleSubjectPicker.SelectedIndex = -1;
                allClassSubjects.Clear();
                multiSubjectItems.Children.Clear();
                streamItems.Children.Clear();

                if (classPicker.SelectedItem is AutoTable.Models.SimpleLookup cls)
                {
                    allClassSubjects.AddRange(await AppServices.DataService!.GetSubjectsForClassAsync(cls.Id));
                    singleSubjectPicker.ItemsSource = allClassSubjects.Select(x => x.Name).ToList();
                    if (singleSubjectPicker.Items.Count > 0) singleSubjectPicker.SelectedIndex = 0;

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
                    var streams = await AppServices.DataService!.GetStreamsForClassAsync(cls.Id);
                    foreach (var st in streams)
                    {
                        streamItems.Children.Add(new CheckBox
                        {
                            Content = st.Name,
                            Tag = st.Id,
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

            if (classes.Count > 0) classPicker.SelectedIndex = 0;

            // Preload all school subjects
            allSchoolSubjects.AddRange(allClassSubjects.Select(s => s.Name));

// â”€â”€ Expandable scope logic â”€â”€
            AssessmentScope? chosenScope = null;

            void UpdateScopeDetails()
            {
                bool isSchool = chosenScope == AssessmentScope.AllInSchool;
                bool isStream = chosenScope == AssessmentScope.Stream;
                bool isClass = chosenScope == AssessmentScope.AllInClass;
                bool chosen = chosenScope.HasValue;

                detailsPanel.Visibility = chosen ? Visibility.Visible : Visibility.Collapsed;
                classPicker.Visibility = (isClass || isStream) ? Visibility.Visible : Visibility.Collapsed;
                streamMultiPanel.Visibility = isStream ? Visibility.Visible : Visibility.Collapsed;

                // Secondary subject granularity applies only to Class / Stream
                subjectScopeBox.Visibility = (isClass || isStream) ? Visibility.Visible : Visibility.Collapsed;
                int sub = subjectScopeBox.SelectedIndex; // 0 single, 1 all, 2 specific
                bool showSubjectTools = (isClass || isStream);
                singleSubjectPicker.Visibility = (showSubjectTools && sub == 0) ? Visibility.Visible : Visibility.Collapsed;
                multiSubjectPanel.Visibility = (showSubjectTools && sub == 2) ? Visibility.Visible : Visibility.Collapsed;

                // Hint text
                scopeHint.Visibility = chosen ? Visibility.Visible : Visibility.Collapsed;
                var clsName = (classPicker.SelectedItem as AutoTable.Models.SimpleLookup)?.Name ?? "the class";
                if (isSchool)
                    scopeHint.Text = "Creates ONE school-wide assessment covering every subject taught; pick the subject at marks entry.";
                else if (isStream)
                {
                    int checkedStreams = streamItems.Children.OfType<CheckBox>().Count(cb => cb.IsChecked == true);
                    scopeHint.Text = checkedStreams == 0
                        ? $"Creates an assessment for {clsName}; check at least one stream below."
                        : $"ONE assessment covering all subjects for {checkedStreams} selected stream(s) in {clsName}. Only those streams' students are listed at marks entry.";
                }
                else if (isClass)
                {
                    scopeHint.Text = sub switch
                    {
                        0 => $"Creates an assessment for one subject in {clsName}.",
                        1 => $"Creates ONE assessment covering all subjects in {clsName}. Pick the subject at marks entry.",
                        _ => "Check the subjects below. ONE assessment is created covering the selected subjects; pick the subject at marks entry."
                    };
                }
            }

            void SetScope(CheckBox sender, AssessmentScope scope)
            {
                chosenScope = scope;
                foreach (var sck in scopeChecks)
                    if (sck != sender) sck.IsChecked = false;
                UpdateScopeDetails();
            }

            entireSchoolCheck.Checked += (s, e) => SetScope(entireSchoolCheck, AssessmentScope.AllInSchool);
            classCheck.Checked += (s, e) => SetScope(classCheck, AssessmentScope.AllInClass);
            streamCheck.Checked += (s, e) => SetScope(streamCheck, AssessmentScope.Stream);
            subjectScopeBox.SelectionChanged += (s, e) => UpdateScopeDetails();

            // Initial state: nothing chosen â†’ details collapsed
            UpdateScopeDetails();

            // â”€â”€ Assemble form â”€â”€
            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(scopePanel);
            panel.Children.Add(detailsPanel);

            classStreamRow.Children.Add(classPicker);

            detailsPanel.Children.Add(nameBox);
            detailsPanel.Children.Add(classStreamRow);
            detailsPanel.Children.Add(streamMultiPanel);
            detailsPanel.Children.Add(subjectScopeBox);
            detailsPanel.Children.Add(singleSubjectPicker);
            detailsPanel.Children.Add(multiSubjectPanel);
            detailsPanel.Children.Add(scopeHint);
            detailsPanel.Children.Add(promoRoleBox);
            detailsPanel.Children.Add(promoRoleHint);
            detailsPanel.Children.Add(weightBox);
            detailsPanel.Children.Add(duePicker);

            var dialog = new ContentDialog
            {
                Title = "Create Assessment",
                // Scrollable content: the modal grows when the scope is chosen, so the
                // form scrolls instead of overflowing the dialog on short screens.
                // MaxHeight must stay BELOW the ContentDialog's internal height cap
                // (~548px) — otherwise the dialog clips the extra height before the
                // ScrollViewer ever gets a chance to show its scrollbar.
                Content = new ScrollViewer
                {
                    MaxHeight = 460,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Content = panel
                },
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
                Width = 580,
                MaxHeight = 620
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var selectedClass = classPicker.SelectedItem as AutoTable.Models.SimpleLookup;
                    if (!chosenScope.HasValue)
                    {
                        var err = new ContentDialog { Title = "Select a scope", Content = "Choose Entire School, Class, or Stream to continue.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                        await err.ShowAsync();
                        return;
                    }
                    var scope = chosenScope == AutoTable.Models.AssessmentScope.AllInSchool
                        ? AutoTable.Models.AssessmentScope.AllInSchool
                        : chosenScope == AutoTable.Models.AssessmentScope.Stream
                            ? AutoTable.Models.AssessmentScope.Stream
                            : (AutoTable.Models.AssessmentScope)subjectScopeBox.SelectedIndex; // 0 Single, 1 AllInClass, 2 SpecificSubjects
                    var selectedStreamIds = scope == AutoTable.Models.AssessmentScope.Stream
                        ? streamItems.Children.OfType<CheckBox>()
                            .Where(cb => cb.IsChecked == true && cb.Tag is int)
                            .Select(cb => (int)cb.Tag!)
                            .ToList()
                        : new List<int>();
                    var weight = int.TryParse(weightBox.Text, out var w) ? w : 0;
                    var assessmentName = nameBox.Text?.Trim() ?? string.Empty;
                    var dueDate = duePicker.Date?.DateTime ?? DateTime.Today;

                    if (string.IsNullOrWhiteSpace(assessmentName))
                    {
                        var err = new ContentDialog { Title = "Name required", Content = "Please enter an assessment name.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                        await err.ShowAsync();
                        return;
                    }

                    // â”€â”€ Resolve promotion role â”€â”€
                    var promoRole = (AutoTable.Models.AssessmentPromotionRole)promoRoleBox.SelectedIndex;

                    // â”€â”€ Resolve subjects covered by this assessment â”€â”€
                    // Every non-single scope creates ONE assessment linked to multiple
                    // subjects (no per-subject duplicates).
                    var coveredSubjects = new List<string>();

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
                        coveredSubjects.Add(subjName);
                    }
                    else if (scope == AutoTable.Models.AssessmentScope.AllInClass)
                    {
                        if (selectedClass == null)
                        {
                            var err = new ContentDialog { Title = "No class selected", Content = "Please select a class.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                        // One assessment covering ALL subjects in the class.
                        coveredSubjects.AddRange(
                            (await AppServices.DataService!.GetSubjectsForClassAsync(selectedClass.Id)).Select(s => s.Name));
                        if (coveredSubjects.Count == 0)
                        {
                            var err = new ContentDialog { Title = "No subjects", Content = $"No subjects assigned to {selectedClass.Name}. Add subjects under Classes Management first.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
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
                        coveredSubjects.AddRange(multiSubjectItems.Children
                            .OfType<CheckBox>()
                            .Where(cb => cb.IsChecked == true)
                            .Select(cb => cb.Content?.ToString() ?? "")
                            .Where(n => !string.IsNullOrWhiteSpace(n)));
                        if (coveredSubjects.Count == 0)
                        {
                            var err = new ContentDialog { Title = "No subjects selected", Content = "Please check at least one subject.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                    }
                    else if (scope == AutoTable.Models.AssessmentScope.AllInSchool)
                    {
                        // One school-wide assessment covering every subject taught in the school.
                        var allClasses = await AppServices.DataService!.GetClassesAsync();
                        foreach (var cls in allClasses)
                        {
                            foreach (var subj in await AppServices.DataService!.GetSubjectsForClassAsync(cls.Id))
                            {
                                if (!coveredSubjects.Contains(subj.Name, StringComparer.OrdinalIgnoreCase))
                                    coveredSubjects.Add(subj.Name);
                            }
                        }
                        if (coveredSubjects.Count == 0)
                        {
                            var err = new ContentDialog { Title = "No subjects", Content = "No subjects are assigned to any class yet.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                    }

                    else if (scope == AutoTable.Models.AssessmentScope.Stream)
                    {
                        if (selectedClass == null)
                        {
                            var err = new ContentDialog { Title = "No class selected", Content = "Please select a class.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                        if (selectedStreamIds.Count == 0)
                        {
                            var err = new ContentDialog { Title = "No stream selected", Content = "Check at least one stream below. Streams are configured under Classes Management.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                        // ONE assessment per stream covering all subjects in the class.
                        coveredSubjects.AddRange(
                            (await AppServices.DataService!.GetSubjectsForClassAsync(selectedClass.Id)).Select(s => s.Name));
                        if (coveredSubjects.Count == 0)
                        {
                            var err = new ContentDialog { Title = "No subjects", Content = $"No subjects assigned to {selectedClass.Name}. Add subjects under Classes Management first.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                            await err.ShowAsync();
                            return;
                        }
                    }

                    // â”€â”€ Create the SINGLE assessment (subject links resolve server-side) â”€â”€
                    var primarySubject = coveredSubjects.Count == 1 ? coveredSubjects[0] : coveredSubjects.First();
                    var classNameForItem = scope == AutoTable.Models.AssessmentScope.AllInSchool
                        ? "All Classes"
                        : selectedClass!.Name;
                    var item = BuildAssessmentItem(assessmentName, classNameForItem, primarySubject, scope, weight, dueDate, selectedStreamIds.Count > 0 ? selectedStreamIds[0] : (int?)null, promoRole);
                    item.SubjectNames = coveredSubjects;
                    if (scope == AutoTable.Models.AssessmentScope.Stream)
                    {
                        // A stream assessment is class-anchored but NOT class-wide; only the
                        // checked streams' students are rosters at marks entry.
                        item.IsClassWide = false;
                        item.StreamIds = selectedStreamIds;
                        item.StreamName = streamItems.Children.OfType<CheckBox>()
                            .Where(cb => cb.IsChecked == true)
                            .Select(cb => cb.Content?.ToString() ?? "")
                            .FirstOrDefault() ?? string.Empty;
                    }
                    var created = await AppServices.DataService!.CreateAssessmentAsync(item);
                    var createdItems = new List<AutoTable.Models.AssessmentItem>();
                    if (created != null) createdItems.Add(created);

                    // Insert all created assessments at top of list
                    for (int i = createdItems.Count - 1; i >= 0; i--)
                    {
                        ViewModel.Assessments.Insert(0, createdItems[i]);
                    }
                    ViewModel.RefreshCounts();

                    var msg = createdItems.Count == 1
                        ? $"Assessment '{createdItems[0].Name}' created for {createdItems[0].Subject}."
                        : $"Created {createdItems.Count} assessments for '{assessmentName}'.";
                    ViewModel.StatusMessage = msg;
                    AppServices.Toasts.Show("Assessment Created", msg);

                    // Audit log
                    await AppServices.Audit.LogAsync("Assessment", "Create", "Assessment",
                        createdItems.Count > 0 ? createdItems[0].Id : null,
                        assessmentName,
                        JsonSerializer.Serialize(new { scope, weight, dueDate }),
                        isSuccess: true);
                }
                catch (Exception ex)
                {
                    var err = new ContentDialog { Title = "Unable to create assessment", Content = ex.Message, CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                    await err.ShowAsync();

                    // Audit log failure
                    await AppServices.Audit.LogAsync("Assessment", "Create", "Assessment",
                        null, nameBox.Text?.Trim() ?? string.Empty, null, isSuccess: false, errorMessage: ex.Message);
                }
            }
        }

        private static AssessmentItem BuildAssessmentItem(
            string name, string className, string subject, AssessmentScope scope,
            int weight, DateTime dueDate, int? streamId,
            AssessmentPromotionRole promoRole = AssessmentPromotionRole.None,
            List<string>? subjectNames = null)
        {
            // Set the author to the current logged-in user
            var currentUser = Services.SessionService.Instance.CurrentUser;
            string? authorName = null;
            if (currentUser != null)
            {
                authorName = currentUser.FullName;
            }

            return new AssessmentItem
            {
                Name = name,
                ClassName = className,
                Subject = subject,
                Scope = scope,
                SubjectNames = subjectNames ?? new List<string>(),
                WeightPercent = weight,
                DueDate = dueDate,
                IsClassWide = true,
                StreamId = streamId,
                PromotionRole = promoRole,
                AuthorName = authorName
            };
        }
    }
}