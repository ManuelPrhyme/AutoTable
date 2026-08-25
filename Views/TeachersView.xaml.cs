using AutoTable.Models;
using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.Views
{
    public sealed partial class TeachersView : Page
    {
        private readonly TeachersViewModel _vm;

        // Modal dimensions matching Walmart old checkout modal reference,
        // expanded 30% wider than the original 800px spec to fit both columns comfortably
        private const double ModalWidth = 1040;   // was 800 (+30%)
        private const double ModalHeight = 577;
        private const double FieldWidth = 408;    // column width reduced 15% (480 × 0.85); dialog width unchanged
        private const double ColumnGap = 24;

        public TeachersView()
        {
            InitializeComponent();
            _vm = new TeachersViewModel();
            DataContext = _vm;
        }

        /// <summary>
        /// Creates a two-column Grid layout matching Walmart checkout modal design.
        /// Left column: Personal info fields. Right column: Assignments & qualifications.
        /// </summary>
        private Grid CreateTwoColumnModalContent()
        {
            var grid = new Grid
            {
                Width = ModalWidth - 48, // Subtract dialog padding
                Height = ModalHeight - 120 // Subtract title + button bar
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColumnGap) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            return grid;
        }

        /// <summary>
        /// Builds a labeled multi-select checkbox flyout control.
        /// Returns a (Container, GetSelectedCsv) tuple.
        /// </summary>
        private (FrameworkElement Container, Func<string> GetSelectedCsv) CreateMultiSelectControl(
            string header, IEnumerable<string> allItems, IEnumerable<string> preSelected)
        {
            var preSelectedSet = new HashSet<string>(preSelected ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            var selectBtn = new Button
            {
                Content = "Select " + header,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            var selectedText = new TextBlock
            {
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = FieldWidth
            };

            var itemsPanel = new StackPanel { Spacing = 4, MaxHeight = 180 };
            var checkBoxes = new List<CheckBox>();
            foreach (var item in allItems)
            {
                var cb = new CheckBox { Content = item, IsChecked = preSelectedSet.Contains(item) };
                checkBoxes.Add(cb);
                itemsPanel.Children.Add(cb);
            }

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = itemsPanel
            };

            var flyout = new Flyout
            {
                Content = scrollViewer,
                Placement = FlyoutPlacementMode.Bottom,
                ShouldConstrainToRootBounds = false
            };

            flyout.Closed += (s, ev) =>
            {
                var selected = checkBoxes.Where(cb => cb.IsChecked == true)
                    .Select(cb => cb.Content?.ToString() ?? "").ToList();
                if (selected.Count > 0)
                {
                    selectBtn.Content = header + " (" + selected.Count + ")";
                    selectedText.Text = string.Join(", ", selected);
                }
                else
                {
                    selectBtn.Content = "Select " + header;
                    selectedText.Text = "None selected";
                }
            };

            selectBtn.Click += (s, ev) => flyout.ShowAt(selectBtn);

            var initialSelected = checkBoxes.Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Content?.ToString() ?? "").ToList();
            if (initialSelected.Count > 0)
            {
                selectBtn.Content = header + " (" + initialSelected.Count + ")";
                selectedText.Text = string.Join(", ", initialSelected);
            }
            else
            {
                selectedText.Text = "None selected";
            }

            var container = new StackPanel { Spacing = 4, Width = FieldWidth };
            container.Children.Add(selectBtn);
            container.Children.Add(selectedText);

            string GetSelectedCsv() => string.Join(", ",
                checkBoxes.Where(cb => cb.IsChecked == true)
                    .Select(cb => cb.Content?.ToString() ?? ""));

            return (container, GetSelectedCsv);
        }

        private async void AddTeacher_Click(object sender, RoutedEventArgs e)
        {
            // Load data for multi-select controls
            var allSubjects = (await AppServices.DataService!.GetSubjectsAsync()).Select(s => s.Name).ToList();
            var allClasses = (await AppServices.DataService!.GetClassesAsync()).Select(c => c.Name).ToList();

            // ── LEFT COLUMN: Personal Information ──
            var leftColumn = new StackPanel { Spacing = 10, Width = FieldWidth };

            var sectionHeader1 = new TextBlock
            {
                Text = "PERSONAL INFORMATION",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Margin = new Thickness(0, 0, 0, 4)
            };
            leftColumn.Children.Add(sectionHeader1);

            var nameBox = new TextBox
            {
                Header = "Full Name",
                PlaceholderText = "e.g. John Doe",
                Width = FieldWidth
            };
            var emailBox = new TextBox
            {
                Header = "Email",
                PlaceholderText = "e.g. john@school.edu",
                Width = FieldWidth
            };
            var phoneBox = new TextBox
            {
                Header = "Phone",
                PlaceholderText = "e.g. 0700123456",
                Width = FieldWidth
            };

            leftColumn.Children.Add(nameBox);
            leftColumn.Children.Add(emailBox);
            leftColumn.Children.Add(phoneBox);

            // ── Next of Kin Section ──
            var sectionHeader2 = new TextBlock
            {
                Text = "NEXT OF KIN",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Margin = new Thickness(0, 8, 0, 4)
            };
            leftColumn.Children.Add(sectionHeader2);

            var nextOfKinNameBox = new TextBox
            {
                Header = "Name",
                PlaceholderText = "e.g. Jane Doe",
                Width = FieldWidth
            };
            var nextOfKinRelBox = new TextBox
            {
                Header = "Relationship",
                PlaceholderText = "e.g. Spouse",
                Width = FieldWidth
            };
            var nextOfKinPhoneBox = new TextBox
            {
                Header = "Phone",
                PlaceholderText = "e.g. 0700654321",
                Width = FieldWidth
            };

            leftColumn.Children.Add(nextOfKinNameBox);
            leftColumn.Children.Add(nextOfKinRelBox);
            leftColumn.Children.Add(nextOfKinPhoneBox);

            // ── RIGHT COLUMN: Assignments & Qualifications ──
            var rightColumn = new StackPanel { Spacing = 10, Width = FieldWidth };

            var sectionHeader3 = new TextBlock
            {
                Text = "ASSIGNMENTS",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Margin = new Thickness(0, 0, 0, 4)
            };
            rightColumn.Children.Add(sectionHeader3);

            var (subjectsControl, getSubjectsCsv) = CreateMultiSelectControl("Subjects", allSubjects, Array.Empty<string>());
            var (classesControl, getClassesCsv) = CreateMultiSelectControl("Classes", allClasses, Array.Empty<string>());

            rightColumn.Children.Add(subjectsControl);
            rightColumn.Children.Add(classesControl);

            // ── Qualifications Section ──
            var sectionHeader4 = new TextBlock
            {
                Text = "QUALIFICATIONS",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Margin = new Thickness(0, 8, 0, 4)
            };
            rightColumn.Children.Add(sectionHeader4);

            var prevSchoolsBox = new TextBox
            {
                Header = "Previous Schools",
                PlaceholderText = "e.g. Kampala Academy",
                Width = FieldWidth
            };
            rightColumn.Children.Add(prevSchoolsBox);

            var togglesPanel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
            var regToggle = new ToggleSwitch { Header = "Registered Teacher", IsOn = false };
            var studentToggle = new ToggleSwitch { Header = "Student Teacher", IsOn = false };
            togglesPanel.Children.Add(regToggle);
            togglesPanel.Children.Add(studentToggle);
            rightColumn.Children.Add(togglesPanel);

            // ── Assemble Grid ──
            var grid = CreateTwoColumnModalContent();
            Grid.SetColumn(leftColumn, 0);
            Grid.SetColumn(rightColumn, 2);
            grid.Children.Add(leftColumn);
            grid.Children.Add(rightColumn);

            // ── Create Dialog ──
            var dialog = new ContentDialog
            {
                Title = "Register New Teacher",
                Content = grid,
                PrimaryButtonText = "Register Teacher",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
                IsPrimaryButtonEnabled = true,
                DefaultButton = ContentDialogButton.Primary
            };
            // WinUI clamps ContentDialog width via ContentDialogMaxWidth (default ~548px),
            // which clips the second column. Raise the cap so the full two-column layout fits.
            dialog.Resources["ContentDialogMaxWidth"] = ModalWidth + 48d;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var name = nameBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name)) return;

                var teacher = new Teacher
                {
                    FullName = name,
                    Email = emailBox.Text?.Trim(),
                    Phone = phoneBox.Text?.Trim(),
                    SubjectsTaught = getSubjectsCsv(),
                    ClassesTaught = getClassesCsv(),
                    NextOfKinName = nextOfKinNameBox.Text?.Trim(),
                    NextOfKinRelationship = nextOfKinRelBox.Text?.Trim(),
                    NextOfKinPhone = nextOfKinPhoneBox.Text?.Trim(),
                    PreviousSchools = prevSchoolsBox.Text?.Trim(),
                    IsRegisteredTeacher = regToggle.IsOn,
                    IsStudentTeacher = studentToggle.IsOn
                };

                try { await _vm.AddTeacherAsync(teacher); }
                catch (Exception ex)
                {
                    var errDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = ex.Message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errDialog.ShowAsync();
                }
            }
        }

        private async void EditTeacher_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int teacherId) return;
            var teacher = _vm.Teachers.FirstOrDefault(t => t.Id == teacherId);
            if (teacher == null) return;

            // Load data for multi-select controls
            var allSubjects = (await AppServices.DataService!.GetSubjectsAsync()).Select(s => s.Name).ToList();
            var allClasses = (await AppServices.DataService!.GetClassesAsync()).Select(c => c.Name).ToList();
            var currentSubjects = ParseCommaSeparated(teacher.SubjectsTaught);
            var currentClasses = ParseCommaSeparated(teacher.ClassesTaught);

            // ── LEFT COLUMN: Personal Information ──
            var leftColumn = new StackPanel { Spacing = 10, Width = FieldWidth };

            var sectionHeader1 = new TextBlock
            {
                Text = "PERSONAL INFORMATION",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Margin = new Thickness(0, 0, 0, 4)
            };
            leftColumn.Children.Add(sectionHeader1);

            var nameBox = new TextBox { Header = "Full Name", Text = teacher.FullName, Width = FieldWidth };
            var emailBox = new TextBox { Header = "Email", Text = teacher.Email, Width = FieldWidth };
            var phoneBox = new TextBox { Header = "Phone", Text = teacher.Phone, Width = FieldWidth };

            leftColumn.Children.Add(nameBox);
            leftColumn.Children.Add(emailBox);
            leftColumn.Children.Add(phoneBox);

            // ── Next of Kin Section ──
            var sectionHeader2 = new TextBlock
            {
                Text = "NEXT OF KIN",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Margin = new Thickness(0, 8, 0, 4)
            };
            leftColumn.Children.Add(sectionHeader2);

            var nextOfKinNameBox = new TextBox { Header = "Name", Text = teacher.NextOfKinName, Width = FieldWidth };
            var nextOfKinRelBox = new TextBox { Header = "Relationship", Text = teacher.NextOfKinRelationship, Width = FieldWidth };
            var nextOfKinPhoneBox = new TextBox { Header = "Phone", Text = teacher.NextOfKinPhone, Width = FieldWidth };

            leftColumn.Children.Add(nextOfKinNameBox);
            leftColumn.Children.Add(nextOfKinRelBox);
            leftColumn.Children.Add(nextOfKinPhoneBox);

            // ── RIGHT COLUMN: Assignments & Qualifications ──
            var rightColumn = new StackPanel { Spacing = 10, Width = FieldWidth };

            var sectionHeader3 = new TextBlock
            {
                Text = "ASSIGNMENTS",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Margin = new Thickness(0, 0, 0, 4)
            };
            rightColumn.Children.Add(sectionHeader3);

            var (subjectsControl, getSubjectsCsv) = CreateMultiSelectControl("Subjects", allSubjects, currentSubjects);
            var (classesControl, getClassesCsv) = CreateMultiSelectControl("Classes", allClasses, currentClasses);

            rightColumn.Children.Add(subjectsControl);
            rightColumn.Children.Add(classesControl);

            // ── Qualifications Section ──
            var sectionHeader4 = new TextBlock
            {
                Text = "QUALIFICATIONS",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Margin = new Thickness(0, 8, 0, 4)
            };
            rightColumn.Children.Add(sectionHeader4);

            var prevSchoolsBox = new TextBox { Header = "Previous Schools", Text = teacher.PreviousSchools, Width = FieldWidth };
            rightColumn.Children.Add(prevSchoolsBox);

            var togglesPanel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
            var regToggle = new ToggleSwitch { Header = "Registered Teacher", IsOn = teacher.IsRegisteredTeacher };
            var studentToggle = new ToggleSwitch { Header = "Student Teacher", IsOn = teacher.IsStudentTeacher };
            togglesPanel.Children.Add(regToggle);
            togglesPanel.Children.Add(studentToggle);
            rightColumn.Children.Add(togglesPanel);

            // ── Assemble Grid ──
            var grid = CreateTwoColumnModalContent();
            Grid.SetColumn(leftColumn, 0);
            Grid.SetColumn(rightColumn, 2);
            grid.Children.Add(leftColumn);
            grid.Children.Add(rightColumn);

            // ── Create Dialog ──
            var dialog = new ContentDialog
            {
                Title = "Edit Teacher \u2014 " + teacher.FullName,
                Content = grid,
                PrimaryButtonText = "Save Changes",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
                IsPrimaryButtonEnabled = true,
                DefaultButton = ContentDialogButton.Primary
            };
            // Same ContentDialogMaxWidth clamp fix as the Register dialog (keeps both columns visible).
            dialog.Resources["ContentDialogMaxWidth"] = ModalWidth + 48d;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var name = nameBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name)) return;
                teacher.FullName = name;
                teacher.Email = emailBox.Text?.Trim();
                teacher.Phone = phoneBox.Text?.Trim();
                teacher.SubjectsTaught = getSubjectsCsv();
                teacher.ClassesTaught = getClassesCsv();
                teacher.NextOfKinName = nextOfKinNameBox.Text?.Trim();
                teacher.NextOfKinRelationship = nextOfKinRelBox.Text?.Trim();
                teacher.NextOfKinPhone = nextOfKinPhoneBox.Text?.Trim();
                teacher.PreviousSchools = prevSchoolsBox.Text?.Trim();
                teacher.IsRegisteredTeacher = regToggle.IsOn;
                teacher.IsStudentTeacher = studentToggle.IsOn;

                try { await _vm.UpdateTeacherAsync(teacher); }
                catch (Exception ex)
                {
                    var errDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = ex.Message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errDialog.ShowAsync();
                }
            }
        }

        private void DeleteTeacher_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int teacherId)
            {
                _vm.DeleteTeacherCommand.Execute(teacherId);
            }
        }

        private static IEnumerable<string> ParseCommaSeparated(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Array.Empty<string>();
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
