using AutoTable.Models;
using AutoTable.Services;
using AutoTable.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace AutoTable.Views
{
    public sealed partial class ClassesView : Page
    {
        private readonly ClassesViewModel _vm;

        public ClassesView()
        {
            InitializeComponent();
            _vm = new ClassesViewModel();
            DataContext = _vm;
            Loaded += ClassesView_Loaded;
        }

        private async void ClassesView_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();          // also loads grading systems
            await _vm.LoadAllStreamsAsync();
        }

        private const string NewGradingSystemOption = "➕ New grading system…";

        // One editable grade-band row inside the grading-system builder
        private sealed class BandInputs
        {
            public TextBox Label = new();
            public TextBox Min = new();
            public TextBox Max = new();
            public CheckBox Pass = new();
            public CheckBox Repeat = new();
            public CheckBox Fail = new();
        }

        private readonly List<BandInputs> _bandRows = new();

        // CREATE CLASS — single modal containing everything: name, class teacher
        // (registered OR student teacher), grading system (pick existing or build
        // new inline), streams and subjects (assign existing or create new inline).
        private async void CreateClass_Click(object sender, RoutedEventArgs e)
        {
            // Any teacher qualifies — registered teachers AND student teachers.
            var teachers = await AppServices.DataService!.GetTeachersAsync();
            if (teachers.Count == 0)
            {
                await ShowErrorAsync("No teachers available.",
                    "Add at least one teacher first under Administration → Teachers. Both registered and student teachers can be assigned.");
                return;
            }

            await _vm.LoadAllStreamsAsync();
            await _vm.LoadGradingSystemsAsync();
            var allSubjects = await AppServices.DataService.GetSubjectsAsync();

            var nameBox = new TextBox { Header = "Class name", PlaceholderText = "e.g. P4", Width = 300 };
            var teacherPicker = new ComboBox
            {
                Header = "Class teacher (registered or student teacher)",
                Width = 300,
                DisplayMemberPath = nameof(AutoTable.Models.Teacher.FullName),
                ItemsSource = teachers,
                Margin = new Thickness(0, 8, 0, 0)
            };

            // Grading system: pick an existing one, or build a new one inline
            var gsItems = new List<object>();
            foreach (var g in _vm.GradingSystems) gsItems.Add(g);
            gsItems.Add(NewGradingSystemOption);

            var gsPicker = new ComboBox
            {
                Header = "Grading system this class will use",
                Width = 300,
                ItemsSource = gsItems,
                SelectedIndex = 0,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var (newGsPanel, gsNameBox, gsDefaultChk, gsPassMarkBox, _) = BuildGradingSystemEditor();
            newGsPanel.Visibility = Visibility.Collapsed;
            gsPicker.SelectionChanged += (_, _) =>
                newGsPanel.Visibility = ReferenceEquals(gsPicker.SelectedItem, NewGradingSystemOption)
                    ? Visibility.Visible : Visibility.Collapsed;

            // Streams & subjects: assign existing or type names for new ones
            var pendingStreams = new ObservableCollection<SimpleLookup>();
            var pendingSubjects = new ObservableCollection<SimpleLookup>();
            var pendingStreamTeachers = new List<int?>(); // tracks teacher Id per stream
            var classTeacherId = teacherPicker.SelectedItem is AutoTable.Models.Teacher ct ? ct.Id : (int?)null;
            var streamsSection = BuildStreamsSection("Streams (optional)", _vm.AllStreams,
                pendingStreams, pendingStreamTeachers, teachers, classTeacherId);
            var subjectsSection = BuildSubjectsSection("Subjects (optional)", allSubjects, pendingSubjects);

            var content = new StackPanel { Spacing = 12 };
            content.Children.Add(nameBox);
            content.Children.Add(teacherPicker);
            content.Children.Add(gsPicker);
            content.Children.Add(newGsPanel);
            foreach (var c in streamsSection.Children.ToList()) { streamsSection.Children.Remove(c); content.Children.Add(c); }
            foreach (var c in subjectsSection.Children.ToList()) { subjectsSection.Children.Remove(c); content.Children.Add(c); }

            var dialog = new ContentDialog
            {
                Title = "New Class",
                Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 540, Content = content },
                PrimaryButtonText = "Create Class",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            var name = nameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await ShowErrorAsync("Class name required.", "Enter a name for the class.");
                return;
            }

            try
            {
                int? teacherId = teacherPicker.SelectedItem is AutoTable.Models.Teacher t ? t.Id : null;
                int? gradingSystemId = null;

                if (gsPicker.SelectedItem is GradingSystemInfo existingGs)
                    gradingSystemId = existingGs.Id;
                else if (ReferenceEquals(gsPicker.SelectedItem, NewGradingSystemOption))
                    gradingSystemId = (await CreateGradingSystemFromEditorAsync(gsNameBox.Text?.Trim(), gsDefaultChk.IsChecked == true,
                        double.TryParse(gsPassMarkBox.Text, out var pm) ? pm : null)).Id;

                var createdClass = await _vm.CreateClassAsync(name, teacherId, gradingSystemId);

                // Attach pending streams/subjects to the freshly created class.
                for (int i = 0; i < pendingStreams.Count; i++)
                {
                    var item = pendingStreams[i];
                    var streamTeacherId = i < pendingStreamTeachers.Count ? pendingStreamTeachers[i] : null;
                    if (streamTeacherId == null) streamTeacherId = classTeacherId; // default to class teacher
                    if (item.Id < 0)
                    {
                        var newStream = await _vm.CreateStreamAsync(item.Name, createdClass.Id);
                        await _vm.AssignStreamToClassAsync(createdClass.Id, newStream.Id, streamTeacherId);
                    }
                    else await _vm.AssignStreamToClassAsync(createdClass.Id, item.Id, streamTeacherId);
                }
                foreach (var item in pendingSubjects)
                {
                    if (item.Id < 0)
                    {
                        var subj = await AppServices.DataService.CreateSubjectAsync(item.Name); // new → create & assign
                        await _vm.AssignSubjectToClassAsync(createdClass.Id, subj.Id);
                    }
                    else await _vm.AssignSubjectToClassAsync(createdClass.Id, item.Id);
                }

                await _vm.LoadAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Unable to create class.", ex.Message);
            }
        }

        // The old select-class detail card was removed; row click is now a no-op hook.
        private async void ClassesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            await Task.CompletedTask;
        }

        // ─────────────────────────────────────────────────────────────
        // GRADING SYSTEMS — standalone creation from the page card and
        // deletion; the same builder is reused inside Create Class.
        // ─────────────────────────────────────────────────────────────
        private async void CreateGradingSystem_Click(object sender, RoutedEventArgs e)
        {
            var (panel, nameBox, defaultChk, passMarkBox, _) = BuildGradingSystemEditor();

            var dialog = new ContentDialog
            {
                Title = "New Grading System",
                Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 480, Content = panel },
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            try
            {
                await CreateGradingSystemFromEditorAsync(nameBox.Text?.Trim(), defaultChk.IsChecked == true,
                    double.TryParse(passMarkBox.Text, out var pm) ? pm : null);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Unable to create grading system.", ex.Message);
            }
        }

        private async void DeleteGradingSystem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int id) return;
            var confirm = new ContentDialog
            {
                Title = "Delete grading system?",
                Content = "Classes using it will fall back to no specific grading system.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
            try
            {
                await AppServices.DataService!.DeleteGradingSystemAsync(id);
                await _vm.LoadGradingSystemsAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Unable to delete grading system.", ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────

        /// <summary>Validates + persists the grading system currently described by _bandRows.</summary>
        private async Task<GradingSystemInfo> CreateGradingSystemFromEditorAsync(string? name, bool isDefault, double? passMark)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Give the grading system a name (e.g. \"PLE\", \"IGCSE\").");

            var bands = new List<(string Label, double Min, double Max, bool Pass, bool Repeat)>();
            foreach (var row in _bandRows)
            {
                var label = row.Label.Text?.Trim();
                if (string.IsNullOrWhiteSpace(label)) continue; // skip empty rows
                double.TryParse(row.Min.Text, out var min);
                double.TryParse(row.Max.Text, out var max);
                if (min > max) (min, max) = (max, min);
                bands.Add((label, min, max, row.Pass.IsChecked == true, row.Repeat.IsChecked == true));
            }
            if (bands.Count == 0)
                throw new InvalidOperationException("Fill in at least one complete grade band row (label + range).");

            var parsedPass = passMark ?? 50;
            if (parsedPass < 0 || parsedPass > 100)
                throw new InvalidOperationException("Pass mark must be between 0 and 100.");

            _bandRows.Clear();
            return await _vm.CreateGradingSystemWithBandsAsync(name, isDefault, bands, parsedPass);
        }

        /// <summary>
        /// Builds the name + default + band-rows editor used both standalone
        /// and inside the Create Class dialog. Populates _bandRows.
        /// </summary>
        private (StackPanel Panel, TextBox NameBox, CheckBox DefaultChk, TextBox PassMarkBox, StackPanel BandsHost) BuildGradingSystemEditor()
        {
            _bandRows.Clear();

            var nameBox = new TextBox { Header = "New system name", PlaceholderText = "e.g. PLE Scale", Width = 300 };
            var defaultChk = new CheckBox { Content = "Use as the school-wide default grading system", Margin = new Thickness(0, 4, 0, 0) };
            // The class author decides the pass mark: a student's terminal average must be
            // at least this percentage to be promoted under this grading system.
            var passMarkBox = new TextBox
            {
                Header = "Pass mark % (average required to be promoted)",
                PlaceholderText = "e.g. 50",
                Text = "50",
                Width = 300,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var headerGrid = new Grid { ColumnSpacing = 8 };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 4; i++) headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            string[] headers = { "Grade label", "Min %", "Max %", "Promotes", "Repeat" };
            for (int i = 0; i < headers.Length; i++)
            {
                var tb = new TextBlock { Text = headers[i], FontSize = 12, VerticalAlignment = VerticalAlignment.Bottom };
                Grid.SetColumn(tb, i);
                headerGrid.Children.Add(tb);
            }

            var bandsHost = new StackPanel { Spacing = 6 };
            AddBandRow(bandsHost);

            var addRowBtn = new Button { Content = "+ Add band row" };
            addRowBtn.Click += (_, _) => AddBandRow(bandsHost);

            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(nameBox);
            panel.Children.Add(defaultChk);
            panel.Children.Add(passMarkBox);
            panel.Children.Add(new TextBlock
            {
                Text = "Grade bands — what each mark range is called, and whether that range promotes or means repeat:",
                Style = (Style)Application.Current.Resources["MutedTextStyle"],
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(headerGrid);
            panel.Children.Add(bandsHost);
            panel.Children.Add(addRowBtn);

            return (panel, nameBox, defaultChk, passMarkBox, bandsHost);
        }

        private void AddBandRow(StackPanel host)
        {
            var row = new BandInputs
            {
                Label = new TextBox { PlaceholderText = "A" },
                Min = new TextBox { PlaceholderText = "90" },
                Max = new TextBox { PlaceholderText = "100" },
                Pass = new CheckBox { Content = "", MinWidth = 0 },
                Repeat = new CheckBox { Content = "", MinWidth = 0 }
            };
            _bandRows.Add(row);

            var grid = new Grid { ColumnSpacing = 8 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 4; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });

            grid.Children.Add(row.Label); Grid.SetColumn(row.Label, 0);
            grid.Children.Add(row.Min); Grid.SetColumn(row.Min, 1);
            grid.Children.Add(row.Max); Grid.SetColumn(row.Max, 2);
            grid.Children.Add(row.Pass); Grid.SetColumn(row.Pass, 3);
            grid.Children.Add(row.Repeat); Grid.SetColumn(row.Repeat, 4);

            host.Children.Add(grid);
        }

        /// <summary>
        /// Builds an "assign existing / type a new name" section for the
        /// Create Class dialog. The "Add →" button moves the picked item —
        /// or a newly typed name (negative Id) — into the pending list.
        /// </summary>
        private static StackPanel BuildSubjectsSection(string title, System.Collections.Generic.IEnumerable<SimpleLookup> source,
            ObservableCollection<SimpleLookup> pending)
        {
            var sourceList = new List<SimpleLookup>(source);
            var picker = new ComboBox
            {
                Width = 220,
                ItemsSource = sourceList,
                DisplayMemberPath = nameof(SimpleLookup.Name),
                PlaceholderText = "Existing…"
            };
            var newBox = new TextBox { PlaceholderText = "or new name…", Width = 220 };

            var subjectsDisplay = new TextBlock
            {
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x33, 0x33, 0x33)),
                Margin = new Thickness(0, 4, 0, 0),
                Text = "No subjects added yet."
            };

            void AddClick(object sender, RoutedEventArgs e)
            {
                if (picker.SelectedItem is SimpleLookup existing)
                {
                    pending.Add(existing);
                    sourceList.Remove(existing);
                    picker.SelectedItem = null;
                }
                else if (!string.IsNullOrWhiteSpace(newBox.Text))
                {
                    var nm = newBox.Text.Trim();
                    if (!pending.Any(p => string.Equals(p.Name, nm, StringComparison.OrdinalIgnoreCase)))
                        pending.Add(new SimpleLookup { Id = -1, Name = nm });
                    newBox.Text = string.Empty;
                }
                subjectsDisplay.Text = pending.Count > 0
                    ? string.Join(", ", pending.Select(p => p.Name))
                    : "No subjects added yet.";
                subjectsDisplay.Opacity = pending.Count > 0 ? 1.0 : 0.5;
            }

            var addBtn = new Button { Content = "Add →" };
            addBtn.Click += AddClick;

            var assignRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            assignRow.Children.Add(picker);
            assignRow.Children.Add(addBtn);

            var newRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            newRow.Children.Add(newBox);

            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 8, 0, 0)
            });
            panel.Children.Add(assignRow);
            panel.Children.Add(newRow);
            panel.Children.Add(subjectsDisplay);
            return panel;
        }

        // ── Streams section: each stream has a stream-teacher ComboBox beside it ──
        // pendingTeachers tracks the chosen teacher Id per pending stream index.
        private static StackPanel BuildStreamsSection(string title, System.Collections.Generic.IEnumerable<SimpleLookup> source,
            ObservableCollection<SimpleLookup> pending, List<int?> pendingTeachers,
            IReadOnlyList<AutoTable.Models.Teacher> allTeachers, int? classTeacherId)
        {
            var picker = new ComboBox
            {
                Width = 180,
                ItemsSource = source,
                DisplayMemberPath = nameof(SimpleLookup.Name),
                PlaceholderText = "Existing…"
            };
            var newBox = new TextBox { PlaceholderText = "or new name…", Width = 180 };

            // Container for stream rows (each row = stream name + teacher ComboBox)
            var streamsContainer = new StackPanel { Spacing = 4, MaxHeight = 180 };

            void RebuildStreamRows()
            {
                streamsContainer.Children.Clear();
                for (int i = 0; i < pending.Count; i++)
                {
                    var idx = i; // capture for closure
                    var streamName = pending[idx].Name;

                    var row = new Grid { ColumnSpacing = 8 };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var nameBlock = new TextBlock
                    {
                        Text = streamName,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        FontSize = 13
                    };
                    Grid.SetColumn(nameBlock, 0);

                    // Stream teacher picker — defaults to class teacher
                    var teacherPicker = new ComboBox
                    {
                        Width = 180,
                        Header = "Stream teacher",
                        PlaceholderText = "(uses class teacher)",
                        ItemsSource = allTeachers,
                        DisplayMemberPath = nameof(AutoTable.Models.Teacher.FullName),
                        Tag = idx
                    };
                    // Pre-select class teacher as default
                    if (classTeacherId.HasValue)
                    {
                        var classTeacher = allTeachers.FirstOrDefault(t => t.Id == classTeacherId.Value);
                        if (classTeacher != null) teacherPicker.SelectedItem = classTeacher;
                    }
                    // Track selection changes
                    teacherPicker.SelectionChanged += (_, args) =>
                    {
                        var i2 = (int)teacherPicker.Tag;
                        if (i2 < pendingTeachers.Count)
                        {
                            pendingTeachers[i2] = teacherPicker.SelectedItem is AutoTable.Models.Teacher t
                                ? t.Id : null;
                        }
                    };
                    Grid.SetColumn(teacherPicker, 1);

                    row.Children.Add(nameBlock);
                    row.Children.Add(teacherPicker);

                    // Remove button
                    var removeBtn = new Button
                    {
                        Content = "✕",
                        Padding = new Thickness(4, 0, 4, 0),
                        FontSize = 11,
                        Tag = idx
                    };
                    removeBtn.Click += (_, _) =>
                    {
                        var ri = (int)removeBtn.Tag;
                        var removed = pending[ri];
                        pending.RemoveAt(ri);
                        if (ri < pendingTeachers.Count) pendingTeachers.RemoveAt(ri);
                        // Return removed stream to the picker if it was an existing one
                        if (removed.Id > 0) picker.Items.Add(removed);
                        RebuildStreamRows();
                    };
                    // Stack the remove button below the row
                    var wrapper = new StackPanel { Spacing = 2 };
                    wrapper.Children.Add(row);
                    wrapper.Children.Add(removeBtn);

                    streamsContainer.Children.Add(wrapper);
                }
            }
            RebuildStreamRows();

            void AddClick(object sender, RoutedEventArgs e)
            {
                if (picker.SelectedItem is SimpleLookup existing)
                {
                    pending.Add(existing);
                    pendingTeachers.Add(null); // default to class teacher
                    picker.Items.Remove(existing);
                    picker.SelectedIndex = -1;
                }
                else if (!string.IsNullOrWhiteSpace(newBox.Text))
                {
                    var nm = newBox.Text.Trim();
                    if (!pending.Any(p => string.Equals(p.Name, nm, StringComparison.OrdinalIgnoreCase)))
                    {
                        pending.Add(new SimpleLookup { Id = -1, Name = nm });
                        pendingTeachers.Add(null);
                    }
                    newBox.Text = string.Empty;
                }
                RebuildStreamRows();
            }

            var addBtn = new Button { Content = "Add →" };
            addBtn.Click += AddClick;

            var assignRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            assignRow.Children.Add(picker);
            assignRow.Children.Add(addBtn);

            var newRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            newRow.Children.Add(newBox);

            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 8, 0, 0)
            });
            panel.Children.Add(assignRow);
            panel.Children.Add(newRow);
            panel.Children.Add(streamsContainer);
            return panel;
        }

        private async Task ShowErrorAsync(string header, string message)
        {
            var dialog = new ContentDialog
            {
                Title = header,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
