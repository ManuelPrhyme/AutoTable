using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoTable.Models;
using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class StudentsView : Page
    {
        private readonly StudentsViewModel _vm;

        /// <summary>Exposes the ViewModel's filtered list so x:Bind can resolve it on the code-behind.</summary>
        public System.Collections.ObjectModel.ObservableCollection<Models.Student> FilteredStudents => _vm.FilteredStudents;

        public StudentsView()
        {
            _vm = new StudentsViewModel();
            DataContext = _vm;
            InitializeComponent();
            Loaded += StudentsView_Loaded;
        }

        private async void StudentsView_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();

            // Populate filter ComboBoxes
            PopulateFilterComboBoxes();
        }

        /// <summary>
        /// Fills the filter ComboBoxes from ViewModel lookup data and sets defaults.
        /// </summary>
        private void PopulateFilterComboBoxes()
        {
            // Status filter — static options
            StatusFilterBox.ItemsSource = new List<string> { "All", "Active", "Inactive" };
            StatusFilterBox.SelectedIndex = 0; // "All"

            // Class filter
            ClassFilterBox.ItemsSource = _vm.FilterClasses;
            ClassFilterBox.DisplayMemberPath = "Name";
            ClassFilterBox.SelectedIndex = 0;

            // Stream filter
            StreamFilterBox.ItemsSource = _vm.FilterStreams;
            StreamFilterBox.DisplayMemberPath = "Name";
            StreamFilterBox.SelectedIndex = 0;

            // Termination year filter (populated when Inactive is selected)
            RefreshTerminationYearFilter();
        }

        /// <summary>
        /// Refreshes the termination year dropdown based on the current student data.
        /// </summary>
        private void RefreshTerminationYearFilter()
        {
            TermYearFilterBox.ItemsSource = null;
            if (_vm.FilterTerminationYears.Count > 0)
            {
                var items = new List<string> { "Any Year" };
                items.AddRange(_vm.FilterTerminationYears.Select(y => y.ToString()));
                TermYearFilterBox.ItemsSource = items;
                TermYearFilterBox.SelectedIndex = 0;
            }
            else
            {
                TermYearFilterBox.ItemsSource = new List<string> { "Any Year" };
                TermYearFilterBox.SelectedIndex = 0;
            }
        }

        private async void AddStudent_Click(object sender, RoutedEventArgs e)
        {
            var form = new EnrollmentFormView();

            ContentDialog? dialog = null;
            form.ViewModel.OnSubmittedAsync = async (createdStudent) =>
            {
                if (createdStudent != null)
                {
                    // Insert newly created student at top of collection so it appears first
                    _vm.Students.Insert(0, createdStudent);
                }
                else
                {
                    // Fallback: reload full list
                    await _vm.LoadAsync();
                }
                _vm.ApplyFilters();
                dialog?.Hide();
            };

            // Modal-size.md standard: 1040 x 577 dialog. WinUI clamps ContentDialog width
            // via ContentDialogMaxWidth (~548px default) — the override is REQUIRED or the
            // two-column layout gets silently clipped.
            dialog = new ContentDialog
            {
                Title = "Enroll New Student",
                Content = form,
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };
            dialog.Resources["ContentDialogMaxWidth"] = 1040d + 48d; // width + padding allowance

            await dialog.ShowAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.Filter = SearchBox.Text;
        }

        // ── Filter event handlers ──

        private void ClassFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            _vm.ClassFilter = ClassFilterBox.SelectedItem as SimpleLookup;
        }

        private void StreamFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            _vm.StreamFilter = StreamFilterBox.SelectedItem as SimpleLookup;
        }

        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            var selected = StatusFilterBox.SelectedItem as string ?? "All";
            _vm.StatusFilter = selected;

            // Show/hide termination year filter when Inactive is selected
            if (selected == "Inactive")
            {
                RefreshTerminationYearFilter();
                TermYearFilterBox.Visibility = Visibility.Visible;
            }
            else
            {
                TermYearFilterBox.Visibility = Visibility.Collapsed;
                _vm.TerminationYearFilter = null;
            }
        }

        private void TermYearFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (TermYearFilterBox.SelectedItem is string yearStr && yearStr != "Any Year" && int.TryParse(yearStr, out var year))
            {
                _vm.TerminationYearFilter = year;
            }
            else
            {
                _vm.TerminationYearFilter = null;
            }
        }

        /// <summary>
        /// Terminates (deactivates) a student: keeps their data and marks them inactive so
        /// they no longer appear in active-student operations. Asks for the cause first.
        /// </summary>
        private async void TerminateStudent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int studentId) return;
            var student = _vm.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return;
            if (!student.IsActive)
            {
                await ShowMessageAsync("Already inactive",
                    $"{student.FullName} is already inactive ({(student.TerminationDate?.ToShortDateString() ?? "no date")}).");
                return;
            }

            // Reason picker: completed course vs. terminated.
            var reasonCombo = new ComboBox
            {
                Header = "Reason",
                Width = 300,
                ItemsSource = new List<KeyValuePair<StudentTerminationReason, string>>
                {
                    new(StudentTerminationReason.Completed, "Completed course"),
                    new(StudentTerminationReason.Expelled, "Terminated (expelled)"),
                    new(StudentTerminationReason.ChangedSchool, "Terminated (left school)"),
                    new(StudentTerminationReason.Other, "Terminated (other)")
                },
                DisplayMemberPath = "Value",
                SelectedIndex = 0
            };

            var panel = new StackPanel { Spacing = 12, MinWidth = 360 };
            panel.Children.Add(new TextBlock
            {
                Text = $"Terminate {student.FullName} ({student.LIN})?",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(reasonCombo);
            panel.Children.Add(new TextBlock
            {
                Text = "The student's records are kept for auditing and past assessments, but they will no longer be counted as an active student in marks, fees, report cards, or dashboards.",
                FontSize = 11,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                TextWrapping = TextWrapping.Wrap
            });

            var dialog = new ContentDialog
            {
                Title = "Terminate Student",
                Content = panel,
                PrimaryButtonText = "Terminate",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var reason = ((KeyValuePair<StudentTerminationReason, string>?)reasonCombo.SelectedItem)?.Key
                         ?? StudentTerminationReason.Expelled;

            try
            {
                await AppServices.DataService!.TerminateStudentAsync(studentId, reason, DateTime.UtcNow, anonymize: false);
                // Reload so the list reflects the new inactive status + cause.
                await _vm.LoadAsync();
                _vm.StatusMessage = $"{student.FullName} marked inactive ({(reason == StudentTerminationReason.Completed ? "completed course" : "terminated")}). Their data is preserved for audit.";
                // AppServices.Toasts.Show("Student Terminated", $"{student.FullName} marked inactive ({(reason == StudentTerminationReason.Completed ? "completed course" : "terminated")}). Records preserved for audit.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Unable to terminate", ex.Message);
            }
        }

        /// <summary>
        /// Opens a modal to shift a student's enrollment to another class/stream. Stream
        /// options are reloaded per the selected class so a stream always belongs to its class.
        /// </summary>
        private async void ShiftEnrollment_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int studentId) return;
            var student = _vm.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return;

            // Load classes first
            await _vm.LoadClassOptionsAsync();

            var classBox = new ComboBox
            {
                Header = "Class",
                ItemsSource = _vm.Classes,
                DisplayMemberPath = "Name",
                SelectedItem = _vm.Classes.FirstOrDefault(c => c.Id == student.ClassId),
                Width = 260
            };
            var streamBox = new ComboBox
            {
                Header = "Stream",
                ItemsSource = _vm.Streams,
                DisplayMemberPath = "Name",
                Width = 260
            };

            // Load streams for the student's current class
            if (student.ClassId.HasValue)
            {
                await _vm.LoadStreamsForClassAsync(student.ClassId.Value);
                streamBox.SelectedItem = _vm.Streams.FirstOrDefault(st => st.Id == student.StreamId);
            }

            // When class selection changes, reload streams for that class
            classBox.SelectionChanged += async (_, _) =>
            {
                if (classBox.SelectedItem is SimpleLookup selectedClass)
                {
                    await _vm.LoadStreamsForClassAsync(selectedClass.Id);
                    streamBox.SelectedItem = null;
                }
            };

            var panel = new StackPanel { Spacing = 12, MinWidth = 320 };
            panel.Children.Add(new TextBlock
            {
                Text = $"{student.FullName} ({student.LIN})",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            panel.Children.Add(classBox);
            panel.Children.Add(streamBox);
            panel.Children.Add(new TextBlock
            {
                Text = "Pick the new class, then the stream within it (optional). This updates the student's active enrollment.",
                FontSize = 11,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                TextWrapping = TextWrapping.Wrap
            });

            var dialog = new ContentDialog
            {
                Title = "Shift Enrollment",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            if (classBox.SelectedItem is not SimpleLookup selClass)
            {
                await ShowMessageAsync("No class selected", "Choose the class to shift the student into.");
                return;
            }

            int? selStreamId = (streamBox.SelectedItem as SimpleLookup)?.Id;

            try
            {
                await _vm.ShiftEnrollmentAsync(student, selClass.Id, selStreamId);
                _vm.StatusMessage = $"Shifted {student.FullName} to {selClass.Name}" +
                                    (selStreamId.HasValue ? $" / {(streamBox.SelectedItem as SimpleLookup)?.Name}" : "") + ".";
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Unable to shift enrollment", ex.Message);
            }
        }

        /// <summary>
        /// Opens a read-only modal showing the student's full record.
        /// </summary>
        private async void ViewStudent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int studentId) return;
            var student = _vm.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return;

            var panel = new StackPanel { Spacing = 10, MinWidth = 420 };
            panel.Children.Add(new TextBlock { Text = $"{student.FullName}", FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontSize = 18 });
            panel.Children.Add(new TextBlock
            {
                Text = $"{student.LIN}  •  {(student.ClassName ?? "—")}  •  {(student.StreamName ?? "—")}",
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextSecondaryBrush"]
            });
            panel.Children.Add(new Border { Height = 1, Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["BorderLightBrush"] });

            var fields = new (string, string)[]
            {
                ("Status", student.StatusLabel),
                ("Admission No.", student.AdmissionNumber ?? "—"),
                ("Date of Birth", student.DateOfBirth?.ToShortDateString() ?? "—"),
                ("Gender", student.Gender ?? "—"),
                ("Guardian", student.GuardianName ?? "—"),
                ("Guardian Phone", student.GuardianPhone ?? "—"),
                ("Guardian Email", student.GuardianEmail ?? "—"),
                ("Guardian Address", student.GuardianAddress ?? "—"),
                ("Guardian Relationship", student.GuardianRelationship ?? "—"),
                ("Residence", $"{student.ResidenceDistrict ?? "—"} / {student.ResidenceZone ?? "—"}"),
                ("Proof of Residence", student.ResidenceProofType ?? "—"),
                ("Allergies/Conditions", student.AllergiesOrConditions ?? "—"),
                ("Health Insurance", student.HealthInsurance ?? "—"),
                ("Emergency Contact", student.EmergencyName ?? "—"),
                ("Emergency Phone", student.EmergencyPhone ?? "—"),
                ("Authorized Pickup", student.AuthorizedPickupPerson ?? "—"),
                ("Enrolled", student.CreatedAt.ToShortDateString()),
                ("Inactive Cause", student.InactiveCauseText),
            };

            foreach (var (label, val) in fields)
            {
                var row = new Grid { ColumnSpacing = 12 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.Children.Add(new TextBlock { Text = label, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextSecondaryBrush"] });
                var valBlock = new TextBlock { Text = val };
                Grid.SetColumn(valBlock, 1);
                row.Children.Add(valBlock);
                panel.Children.Add(row);
            }

            var dialog = new ContentDialog
            {
                Title = "Student Details",
                Content = new ScrollViewer { MaxHeight = 480, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel },
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }


        /// <summary>
        /// Opens a modal to edit the student's core information, then saves via the data
        /// service and reloads the list.
        /// </summary>
        private async void EditStudent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int studentId) return;
            var student = _vm.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return;

            var nameBox = new TextBox { Header = "Full name", Text = student.FullName, Width = 320 };
            var linBox = new TextBox { Header = "LIN", Text = student.LIN, Width = 320 };
            var genderBox = new TextBox { Header = "Gender", Text = student.Gender ?? "", Width = 320 };
            var dobBox = new CalendarDatePicker { Header = "Date of birth", Date = student.DateOfBirth ?? DateTime.Today, Width = 320, HorizontalAlignment = HorizontalAlignment.Left };
            var guardianBox = new TextBox { Header = "Guardian name", Text = student.GuardianName ?? "", Width = 320 };
            var gPhoneBox = new TextBox { Header = "Guardian phone", Text = student.GuardianPhone ?? "", Width = 320 };
            var gEmailBox = new TextBox { Header = "Guardian email", Text = student.GuardianEmail ?? "", Width = 320 };
            var emNameBox = new TextBox { Header = "Emergency contact", Text = student.EmergencyName ?? "", Width = 320 };
            var emPhoneBox = new TextBox { Header = "Emergency phone", Text = student.EmergencyPhone ?? "", Width = 320 };
            var allergyBox = new TextBox { Header = "Allergies / conditions", Text = student.AllergiesOrConditions ?? "", Width = 320 };

            var panel = new StackPanel { Spacing = 10, MinWidth = 360 };
            panel.Children.Add(nameBox);
            panel.Children.Add(linBox);
            panel.Children.Add(genderBox);
            panel.Children.Add(dobBox);
            panel.Children.Add(guardianBox);
            panel.Children.Add(gPhoneBox);
            panel.Children.Add(gEmailBox);
            panel.Children.Add(emNameBox);
            panel.Children.Add(emPhoneBox);
            panel.Children.Add(allergyBox);

            var dialog = new ContentDialog
            {
                Title = $"Edit {student.FullName}",
                Content = new ScrollViewer { MaxHeight = 460, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel },
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                student.FullName = nameBox.Text?.Trim() ?? student.FullName;
                student.LIN = linBox.Text?.Trim() ?? student.LIN;
                student.Gender = string.IsNullOrWhiteSpace(genderBox.Text) ? null : genderBox.Text.Trim();
                student.DateOfBirth = dobBox.Date?.DateTime;
                student.GuardianName = string.IsNullOrWhiteSpace(guardianBox.Text) ? null : guardianBox.Text.Trim();
                student.GuardianPhone = string.IsNullOrWhiteSpace(gPhoneBox.Text) ? null : gPhoneBox.Text.Trim();
                student.GuardianEmail = string.IsNullOrWhiteSpace(gEmailBox.Text) ? null : gEmailBox.Text.Trim();
                student.EmergencyName = string.IsNullOrWhiteSpace(emNameBox.Text) ? null : emNameBox.Text.Trim();
                student.EmergencyPhone = string.IsNullOrWhiteSpace(emPhoneBox.Text) ? null : emPhoneBox.Text.Trim();
                student.AllergiesOrConditions = string.IsNullOrWhiteSpace(allergyBox.Text) ? null : allergyBox.Text.Trim();

                await AppServices.DataService!.UpdateStudentAsync(student);
                _vm.ApplyFilters();
                _vm.StatusMessage = $"{student.FullName} updated.";
                // AppServices.Toasts.Show("Student Updated", $"{student.FullName}'s information was updated.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Unable to update student", ex.Message);
            }
        }


        private async Task ShowMessageAsync(string title, string content)
        {
            await new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }
    }
}