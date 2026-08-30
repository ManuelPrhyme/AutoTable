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
        // ── ROLE-BASED GATING (dormant during development) ──────
        // Uncomment _isAdmin and the guards below when enforcing role restrictions.
        // private readonly bool _isAdmin;

        public ClassesView()
        {
            InitializeComponent();
            _vm = new ClassesViewModel();
            DataContext = _vm;
            // _isAdmin = SessionService.Instance.IsAdministrator;
            Loaded += ClassesView_Loaded;
        }

        private async void ClassesView_Loaded(object sender, RoutedEventArgs e)
        {
            // ── ROLE-BASED GATING (dormant during development) ──────
            // Hide create/edit/grading-system buttons for non-admins:
            // if (!_isAdmin)
            // {
            //     CreateClassButton.Visibility = Visibility.Collapsed;
            //     CreateGradingSystemButton.Visibility = Visibility.Collapsed;
            // }

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
            // ── ROLE-BASED GATING (dormant during development) ──────
            // if (!_isAdmin)
            // {
            //     await ShowErrorAsync("Access Restricted", "Only administrators can create classes.");
            //     return;
            // }

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

            // Streams & subjects: assign existing or type names for new ones.
            // Pass creation callbacks so new items are persisted to the DB immediately,
            // appearing in the master list for future use.
            var pendingStreams = new ObservableCollection<SimpleLookup>();
            var pendingSubjects = new ObservableCollection<SimpleLookup>();
            var pendingStreamTeachers = new List<int?>(); // tracks teacher Id per stream
            var classTeacherId = teacherPicker.SelectedItem is AutoTable.Models.Teacher ct ? ct.Id : (int?)null;
            var streamsSection = BuildStreamsSection("Streams (optional)", _vm.AllStreams,
                pendingStreams, pendingStreamTeachers, teachers, classTeacherId,
                createItemAsync: async name => await AppServices.DataService!.CreateStreamAsync(name));
            var subjectsSection = BuildSubjectsSection("Subjects (optional)", allSubjects, pendingSubjects,
                createItemAsync: async name => await AppServices.DataService!.CreateSubjectAsync(name));

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
                    await _vm.AssignStreamToClassAsync(createdClass.Id, item.Id, streamTeacherId);
                }
                foreach (var item in pendingSubjects)
                {
                    await _vm.AssignSubjectToClassAsync(createdClass.Id, item.Id);
                }

                // AppServices.Toasts.Show("Class Created", $"Class '{createdClass.Name}' created.");

                await _vm.LoadAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Unable to create class.", ex.Message);
            }
        }

        private async void ClassesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ClassInfo cls)
                await OpenEditClassModalAsync(cls);
        }

        private async void EditClass_Click(object sender, RoutedEventArgs e)
        {
            // ── ROLE-BASED GATING (dormant during development) ──────
            // if (!_isAdmin)
            // {
            //     await ShowErrorAsync("Access Restricted", "Only administrators can edit classes.");
            //     return;
            // }
            if ((sender as Button)?.Tag is ClassInfo cls)
                await OpenEditClassModalAsync(cls);
        }

        /// <summary>
        /// Opens a modal to edit the given class: name, class teacher, grading system,
        /// add/remove streams and subjects.
        /// </summary>
        private async Task OpenEditClassModalAsync(ClassInfo cls)
        {
            var teachers = await AppServices.DataService!.GetTeachersAsync();
            await _vm.LoadAllStreamsAsync();
            await _vm.LoadGradingSystemsAsync();
            await _vm.LoadSubjectsForClassAsync(cls.Id);
            var allSubjects = await AppServices.DataService.GetSubjectsAsync();

            // --- Name field ---
            var nameBox = new TextBox
            {
                Header = "Class name",
                Text = cls.Name,
                Width = 300
            };

            // --- Teacher picker ---
            var teacherPicker = new ComboBox
            {
                Header = "Class teacher (registered or student teacher)",
                Width = 300,
                DisplayMemberPath = nameof(AutoTable.Models.Teacher.FullName),
                ItemsSource = teachers,
                Margin = new Thickness(0, 8, 0, 0)
            };
            if (cls.ClassTeacherId.HasValue)
            {
                var current = teachers.FirstOrDefault(t => t.Id == cls.ClassTeacherId.Value);
                if (current != null) teacherPicker.SelectedItem = current;
            }

            // --- Grading system picker ---
            var gsItems = new List<object>();
            foreach (var g in _vm.GradingSystems) gsItems.Add(g);
            gsItems.Add(NewGradingSystemOption);
            var gsPicker = new ComboBox
            {
                Header = "Grading system",
                Width = 300,
                ItemsSource = gsItems,
                Margin = new Thickness(0, 8, 0, 0)
            };
            // Pre-select the current grading system
            if (cls.GradingSystemId.HasValue)
            {
                var currentGs = _vm.GradingSystems.FirstOrDefault(g => g.Id == cls.GradingSystemId.Value);
                if (currentGs != null) gsPicker.SelectedItem = currentGs;
                else gsPicker.SelectedIndex = 0;
            }
            else gsPicker.SelectedIndex = 0;

            var (newGsPanel, gsNameBox, gsDefaultChk, gsPassMarkBox, _) = BuildGradingSystemEditor();
            newGsPanel.Visibility = Visibility.Collapsed;
            gsPicker.SelectionChanged += (_, _) =>
                newGsPanel.Visibility = ReferenceEquals(gsPicker.SelectedItem, NewGradingSystemOption)
                    ? Visibility.Visible : Visibility.Collapsed;

            // --- Streams & subjects ---
            var currentStreams = new ObservableCollection<SimpleLookup>(_vm.StreamsForClass);
            var currentSubjects = new ObservableCollection<SimpleLookup>(_vm.SubjectsForClass);
            var pendingStreamTeachers = new List<int?>();

            // Resolve the current class teacher for default stream teacher
            var classTeacherId = cls.ClassTeacherId;

            var streamsSection = BuildStreamsSection(
                $"Streams for {cls.Name}", _vm.AllStreams,
                currentStreams, pendingStreamTeachers, teachers, classTeacherId,
                createItemAsync: async name => await AppServices.DataService!.CreateStreamAsync(name));
            var subjectsSection = BuildSubjectsSection(
                $"Subjects for {cls.Name}", allSubjects,
                currentSubjects,
                createItemAsync: async name => await AppServices.DataService!.CreateSubjectAsync(name));

            // --- Build dialog content ---
            var content = new StackPanel { Spacing = 12 };
            content.Children.Add(nameBox);
            content.Children.Add(teacherPicker);
            content.Children.Add(gsPicker);
            content.Children.Add(newGsPanel);
            foreach (var c in streamsSection.Children.ToList()) { streamsSection.Children.Remove(c); content.Children.Add(c); }
            content.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
                Margin = new Thickness(0, 4, 0, 4)
            });
            foreach (var c in subjectsSection.Children.ToList()) { subjectsSection.Children.Remove(c); content.Children.Add(c); }

            var statusText = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
                Margin = new Thickness(0, 4, 0, 0)
            };
            content.Children.Add(statusText);

            var dialog = new ContentDialog
            {
                Title = $"Edit Class: {cls.Name}",
                Content = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    MaxHeight = 600,
                    Content = content
                },
                PrimaryButtonText = "Save Changes",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            try
            {
                // --- Update class name, teacher, grading system ---
                var newName = nameBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(newName))
                {
                    await ShowErrorAsync("Class name required.", "Enter a name for the class.");
                    return;
                }
                int? newTeacherId = teacherPicker.SelectedItem is AutoTable.Models.Teacher t ? t.Id : null;
                int? newGsId = null;
                if (gsPicker.SelectedItem is GradingSystemInfo existingGs)
                    newGsId = existingGs.Id;
                else if (ReferenceEquals(gsPicker.SelectedItem, NewGradingSystemOption))
                    newGsId = (await CreateGradingSystemFromEditorAsync(gsNameBox.Text?.Trim(), gsDefaultChk.IsChecked == true,
                        double.TryParse(gsPassMarkBox.Text, out var pm) ? pm : null)).Id;

                await _vm.UpdateClassAsync(cls.Id, newName, newTeacherId, newGsId);

                // --- Reconcile streams ---
                var originalStreams = (await AppServices.DataService.GetStreamsForClassAsync(cls.Id)).ToList();
                var finalStreamIds = currentStreams.Select(s => s.Id).ToHashSet();
                var originalStreamIds = originalStreams.Select(s => s.Id).ToHashSet();

                // Remove streams that were in original but not in final
                foreach (var orig in originalStreams)
                {
                    if (!finalStreamIds.Contains(orig.Id))
                        await _vm.RemoveStreamFromClassAsync(cls.Id, orig.Id);
                }
                // Add streams that are in final but not in original
                for (int i = 0; i < currentStreams.Count; i++)
                {
                    var item = currentStreams[i];
                    var streamTeacherId = i < pendingStreamTeachers.Count ? pendingStreamTeachers[i] : classTeacherId;
                    if (!originalStreamIds.Contains(item.Id))
                    {
                        if (item.Id < 0)
                        {
                            var newStream = await _vm.CreateStreamAsync(item.Name, cls.Id);
                            await _vm.AssignStreamToClassAsync(cls.Id, newStream.Id, streamTeacherId);
                        }
                        else
                        {
                            await _vm.AssignStreamToClassAsync(cls.Id, item.Id, streamTeacherId);
                        }
                    }
                }

                // --- Reconcile subjects ---
                var originalSubjects = (await AppServices.DataService.GetSubjectsForClassAsync(cls.Id)).ToList();
                var finalSubjectIds = currentSubjects.Select(s => s.Id).ToHashSet();
                var originalSubjectIds = originalSubjects.Select(s => s.Id).ToHashSet();

                foreach (var orig in originalSubjects)
                {
                    if (!finalSubjectIds.Contains(orig.Id))
                        await _vm.RemoveSubjectFromClassAsync(cls.Id, orig.Id);
                }
                foreach (var item in currentSubjects)
                {
                    if (!originalSubjectIds.Contains(item.Id))
                    {
                        if (item.Id < 0)
                        {
                            var newSubj = await AppServices.DataService.CreateSubjectAsync(item.Name);
                            await _vm.AssignSubjectToClassAsync(cls.Id, newSubj.Id);
                        }
                        else
                        {
                            await _vm.AssignSubjectToClassAsync(cls.Id, item.Id);
                        }
                    }
                }

                await _vm.LoadAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Unable to update class.", ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // GRADING SYSTEMS — standalone creation from the page card and
        // deletion; the same builder is reused inside Create Class.
        // ─────────────────────────────────────────────────────────────
        private async void CreateGradingSystem_Click(object sender, RoutedEventArgs e)
        {
            // ── ROLE-BASED GATING (dormant during development) ──────
            // if (!_isAdmin)
            // {
            //     await ShowErrorAsync("Access Restricted", "Only administrators can create grading systems.");
            //     return;
            // }

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
            // ── ROLE-BASED GATING (dormant during development) ──────
            // if (!_isAdmin)
            // {
            //     await ShowErrorAsync("Access Restricted", "Only administrators can delete grading systems.");
            //     return;
            // }
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
        /// Create / Edit Class dialog. Shows each added subject as a white
        /// chip with an ✕ remove button. The "Add →" button moves the picked
        /// item — or a newly typed name (negative Id) — into the pending list.
        /// When createItemAsync is provided, newly typed names are persisted to
        /// the DB immediately so they appear in the master list for future use.
        /// </summary>
        private static StackPanel BuildSubjectsSection(string title, System.Collections.Generic.IEnumerable<SimpleLookup> source,
            ObservableCollection<SimpleLookup> pending, Func<string, Task<SimpleLookup>>? createItemAsync = null)
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

            // Container for subject chips — rebuilt on every add/remove
            var chipsHost = new ItemsControl();

            void RebuildChips()
            {
                chipsHost.Items.Clear();
                foreach (var item in pending.ToList())
                {
                    var chip = new Grid { Margin = new Thickness(0, 0, 6, 4) };
                    chip.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    chip.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var nameBlock = new TextBlock
                    {
                        Text = item.Name,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 13,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                        Padding = new Thickness(8, 4, 4, 4)
                    };
                    Grid.SetColumn(nameBlock, 0);

                    var removeBtn = new Button
                    {
                        Content = "✕",
                        Padding = new Thickness(4, 0, 6, 0),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
                        BorderThickness = new Thickness(0),
                        Tag = item,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    removeBtn.Click += (_, _) =>
                    {
                        pending.Remove(item);
                        if (item.Id > 0 && !sourceList.Any(s => s.Id == item.Id))
                            sourceList.Add(item);
                        RebuildChips();
                    };
                    Grid.SetColumn(removeBtn, 1);

                    chip.Children.Add(nameBlock);
                    chip.Children.Add(removeBtn);
                    chipsHost.Items.Add(new Border
                    {
                        Child = chip,
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x33, 0x55, 0x77)),
                        CornerRadius = new CornerRadius(16),
                        Padding = new Thickness(2, 0, 0, 0)
                    });
                }
                if (pending.Count == 0)
                {
                    chipsHost.Items.Add(new TextBlock
                    {
                        Text = "No subjects added yet.",
                        Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
                        FontSize = 13,
                        Margin = new Thickness(0, 4, 0, 0)
                    });
                }
            }
            RebuildChips();

            async void AddClick(object sender, RoutedEventArgs e)
            {
                // Priority: if the user typed a name, always use it (new or existing).
                // Only fall back to the picker when newBox is empty.
                if (!string.IsNullOrWhiteSpace(newBox.Text))
                {
                    var nm = newBox.Text.Trim();
                    if (!pending.Any(p => string.Equals(p.Name, nm, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (createItemAsync != null)
                        {
                            try
                            {
                                var created = await createItemAsync(nm);
                                pending.Add(created);
                                if (!sourceList.Any(s => s.Id == created.Id))
                                    sourceList.Add(created);
                            }
                            catch { pending.Add(new SimpleLookup { Id = -1, Name = nm }); }
                        }
                        else
                        {
                            pending.Add(new SimpleLookup { Id = -1, Name = nm });
                        }
                    }
                    newBox.Text = string.Empty;
                }
                else if (picker.SelectedItem is SimpleLookup existing)
                {
                    pending.Add(existing);
                    sourceList.Remove(existing);
                    picker.SelectedItem = null;
                }
                RebuildChips();
            }

            var addBtn = new Button { Content = "Add →", Style = (Style)Application.Current.Resources["SecondaryButtonStyle"] };
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
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0))
            });
            panel.Children.Add(assignRow);
            panel.Children.Add(newRow);
            panel.Children.Add(chipsHost);
            return panel;
        }

        // ── Streams section: each stream shows as a white chip with ✕ and an optional teacher picker ──
        // pendingTeachers tracks the chosen teacher Id per pending stream index.
        private static StackPanel BuildStreamsSection(string title, System.Collections.Generic.IEnumerable<SimpleLookup> source,
            ObservableCollection<SimpleLookup> pending, List<int?> pendingTeachers,
            IReadOnlyList<AutoTable.Models.Teacher> allTeachers, int? classTeacherId,
            Func<string, Task<SimpleLookup>>? createItemAsync = null)
        {
            var sourceList = new List<SimpleLookup>(source);
            var picker = new ComboBox
            {
                Width = 180,
                ItemsSource = sourceList,
                DisplayMemberPath = nameof(SimpleLookup.Name),
                PlaceholderText = "Existing…"
            };
            var newBox = new TextBox { PlaceholderText = "or new name…", Width = 180 };

            // Container for stream chips — rebuilt on every add/remove
            var chipsHost = new ItemsControl();

            void RebuildStreamChips()
            {
                chipsHost.Items.Clear();
                for (int i = 0; i < pending.Count; i++)
                {
                    var idx = i; // capture for closure
                    var item = pending[idx];

                    var chip = new Grid { ColumnSpacing = 6, Margin = new Thickness(0, 0, 6, 4) };
                    chip.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    chip.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    chip.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var nameBlock = new TextBlock
                    {
                        Text = item.Name,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 13,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                        Padding = new Thickness(8, 4, 4, 4)
                    };
                    Grid.SetColumn(nameBlock, 0);

                    // Stream teacher picker — defaults to class teacher
                    var teacherPicker = new ComboBox
                    {
                        Width = 160,
                        Header = "",
                        PlaceholderText = "(class teacher)",
                        ItemsSource = allTeachers,
                        DisplayMemberPath = nameof(AutoTable.Models.Teacher.FullName),
                        Tag = idx,
                        FontSize = 11
                    };
                    if (classTeacherId.HasValue)
                    {
                        var classTeacher = allTeachers.FirstOrDefault(t => t.Id == classTeacherId.Value);
                        if (classTeacher != null) teacherPicker.SelectedItem = classTeacher;
                    }
                    teacherPicker.SelectionChanged += (_, _) =>
                    {
                        var i2 = (int)teacherPicker.Tag;
                        if (i2 < pendingTeachers.Count)
                        {
                            pendingTeachers[i2] = teacherPicker.SelectedItem is AutoTable.Models.Teacher t
                                ? t.Id : null;
                        }
                    };
                    Grid.SetColumn(teacherPicker, 1);

                    var removeBtn = new Button
                    {
                        Content = "✕",
                        Padding = new Thickness(4, 0, 6, 0),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
                        BorderThickness = new Thickness(0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    removeBtn.Click += (_, _) =>
                    {
                        var removed = pending[idx];
                        pending.RemoveAt(idx);
                        if (idx < pendingTeachers.Count) pendingTeachers.RemoveAt(idx);
                        if (removed.Id > 0 && !sourceList.Any(s => s.Id == removed.Id))
                            sourceList.Add(removed);
                        RebuildStreamChips();
                    };
                    Grid.SetColumn(removeBtn, 2);

                    chip.Children.Add(nameBlock);
                    chip.Children.Add(teacherPicker);
                    chip.Children.Add(removeBtn);
                    chipsHost.Items.Add(new Border
                    {
                        Child = chip,
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x22, 0x44, 0x66)),
                        CornerRadius = new CornerRadius(16),
                        Padding = new Thickness(2, 0, 0, 0)
                    });
                }
                if (pending.Count == 0)
                {
                    chipsHost.Items.Add(new TextBlock
                    {
                        Text = "No streams added yet.",
                        Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
                        FontSize = 13,
                        Margin = new Thickness(0, 4, 0, 0)
                    });
                }
            }
            RebuildStreamChips();

            async void AddClick(object sender, RoutedEventArgs e)
            {
                // Priority: if the user typed a name, always use it (new or existing).
                // Only fall back to the picker when newBox is empty.
                if (!string.IsNullOrWhiteSpace(newBox.Text))
                {
                    var nm = newBox.Text.Trim();
                    if (!pending.Any(p => string.Equals(p.Name, nm, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (createItemAsync != null)
                        {
                            try
                            {
                                var created = await createItemAsync(nm);
                                pending.Add(created);
                                pendingTeachers.Add(null);
                                if (!sourceList.Any(s => s.Id == created.Id))
                                    sourceList.Add(created);
                            }
                            catch { pending.Add(new SimpleLookup { Id = -1, Name = nm }); pendingTeachers.Add(null); }
                        }
                        else
                        {
                            pending.Add(new SimpleLookup { Id = -1, Name = nm });
                            pendingTeachers.Add(null);
                        }
                    }
                    newBox.Text = string.Empty;
                }
                else if (picker.SelectedItem is SimpleLookup existing)
                {
                    pending.Add(existing);
                    pendingTeachers.Add(null);
                    sourceList.Remove(existing);
                    picker.SelectedItem = null;
                }
                RebuildStreamChips();
            }

            var addBtn = new Button { Content = "Add →", Style = (Style)Application.Current.Resources["SecondaryButtonStyle"] };
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
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xE0, 0xE0, 0xE0))
            });
            panel.Children.Add(assignRow);
            panel.Children.Add(newRow);
            panel.Children.Add(chipsHost);
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
