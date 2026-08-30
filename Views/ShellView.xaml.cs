using AutoTable.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoTable.ViewModels;
using AutoTable.Views;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using Windows.Storage.Streams;

namespace AutoTable.Views
{
    public sealed partial class ShellView : Page
    {
        private readonly ShellViewModel _vm;
        private Button? _activeNavButton;

        private static readonly Dictionary<string, (string Title, string Subtitle)> PageMeta = new()
        {
            ["Dashboard"]          = ("Marks Management Dashboard", "Overview of assessments, marks entry, and student performance."),
            ["Assessments"]        = ("Assessments", "Create, track, and manage class assessments and weightings."),
            ["MarksEntry"]         = ("Marks Entry", "Enter and update student marks for selected assessments."),
            ["Gradebook"]          = ("Gradebook", "View consolidated marks, averages, ranks, and grades by class."),
            ["StudentPerformance"] = ("Student Performance", "Individual student performance summaries and trends."),
            ["Analytics"]          = ("Analytics", "Hierarchical performance analytics across classes and time."),
            ["Moderation"]         = ("Moderation", "Review and approve marks before publishing."),
            ["ReportCards"]        = ("Report Cards", "Generate and print student report cards."),
            ["Teachers"]           = ("Teachers", "Register and manage teachers."),
            ["Students"]           = ("Students", "Manage student records, LIN identifiers, and termination."),
            ["Classes"]            = ("Classes & Subjects", "Manage classes, subjects, and subject assignments."),
            ["Promotion"]          = ("Promotion / Repeat", "End-of-year (Term 3) promote or repeat decisions per student."),
            ["AuditLog"]           = ("Termination / Audit Log", "View termination history and anonymization records."),
            ["FinDashboard"]       = ("Financial Dashboard", "Overview of fee collection, budget, and expenditure."),
            ["FeeCollection"]      = ("Fee Collection", "Track and manage student fee payments."),
            ["Budget"]             = ("Budget & Expenditure", "School budget planning and expenditure tracking."),
            ["AiInsights"]         = ("AI Insights", "AI-assisted recommendations and workflow automations."),
        };

        private static readonly Dictionary<string, System.Type> Routes = new()
        {
            ["Dashboard"]          = typeof(DashboardView),
            ["Assessments"]        = typeof(AssessmentsView),
            ["MarksEntry"]         = typeof(MarksEntryView),
            ["Gradebook"]          = typeof(GradebookView),
            ["StudentPerformance"] = typeof(StudentPerformanceView),
            ["Analytics"]          = typeof(AnalyticsView),
            ["Moderation"]         = typeof(ModerationView),
            ["ReportCards"]        = typeof(ReportCardsView),
            ["Teachers"]           = typeof(TeachersView),
            ["Students"]           = typeof(StudentsView),
            ["Classes"]            = typeof(ClassesView),
            ["Promotion"]          = typeof(PromotionView),
            ["AuditLog"]           = typeof(AuditLogView),
            ["TermManagement"]     = typeof(TermManagementView),
            ["FinDashboard"]       = typeof(FinancialsDashboardView),
            ["FeeCollection"]      = typeof(FeeCollectionView),
            ["Budget"]             = typeof(BudgetView),
            ["AiInsights"]         = typeof(AiInsightsView),
            ["SchoolSettings"]     = typeof(SchoolSettingsView),
            ["Defaulters"]         = typeof(DefaultersAnalyticsView),
        };

        public ShellView()
        {
            InitializeComponent();
            _vm = new ShellViewModel();
            DataContext = _vm;
            Loaded += ShellView_Loaded;
            ThemeToggle.Toggled += ThemeToggle_Toggled;
        }

        private async void ShellView_Loaded(object sender, RoutedEventArgs e)
        {
            var user = SessionService.Instance.CurrentUser;
            SidebarUserName.Text = user?.FullName ?? "User";
            SidebarUserRole.Text = _vm.UserRoleLabel;
            SidebarAvatar.DisplayName = user?.FullName ?? "U";
            HeaderUserName.Text = user?.FullName ?? "User";

            // Load school branding (name, motto, logo) from settings
            try
            {
                var ds = AppServices.DataService;
                if (ds != null)
                {
                    var settings = await ds.GetSchoolSettingsAsync();
                    if (!string.IsNullOrWhiteSpace(settings.SchoolName))
                        SchoolNameText.Text = settings.SchoolName;
                    if (!string.IsNullOrWhiteSpace(settings.Motto))
                        SchoolMottoText.Text = settings.Motto;
                    ApplySchoolLogo(settings.LogoBytes);
                }
            }
            catch { }

            // ── ROLE-BASED SIDEBAR GATING (dormant during development) ──────
            // Uncomment the block below and remove the unconditional Visible lines
            // when you switch from development mode to production role enforcement.
            //
            // Admin-only pages: hide sidebar items for non-admins
            // NavModeration.Visibility = _vm.IsAdministrator
            //     ? Visibility.Visible : Visibility.Collapsed;
            // NavTermManagement.Visibility = _vm.IsAdministrator
            //     ? Visibility.Visible : Visibility.Collapsed;
            // NavClasses.Visibility = _vm.IsAdministrator
            //     ? Visibility.Visible : Visibility.Collapsed;
            // NavBudget.Visibility = _vm.IsAdministrator
            //     ? Visibility.Visible : Visibility.Collapsed;
            // NavPromotion.Visibility = _vm.IsAdministrator
            //     ? Visibility.Visible : Visibility.Collapsed;

            // DEV MODE: all pages visible to all roles
            NavModeration.Visibility = Visibility.Visible;
            NavTermManagement.Visibility = Visibility.Visible;
            NavClasses.Visibility = Visibility.Visible;
            NavBudget.Visibility = Visibility.Visible;
            NavPromotion.Visibility = Visibility.Visible;

            NavigationService.Instance.InitializeShell(ContentFrame);

            // Keep header/sidebar in sync when other pages navigate the shell frame
            // (e.g. Dashboard Quick Actions, top-search suggestions).
            NavigationService.Instance.ShellNavigated -= OnExternalShellNavigated;
            NavigationService.Instance.ShellNavigated += OnExternalShellNavigated;
            Unloaded += (_, _) => NavigationService.Instance.ShellNavigated -= OnExternalShellNavigated;

            NavigateTo("Dashboard", NavDashboard);
        }

        /// <summary>
        /// Shows the school logo loaded from Settings in the sidebar; the blue glyph
        /// placeholder is used when no logo has been configured.
        /// </summary>
        private void ApplySchoolLogo(byte[]? logoBytes)
        {
            if (logoBytes != null && logoBytes.Length > 0)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    using var ms = new MemoryStream(logoBytes);
                    bitmap.SetSource(ms.AsRandomAccessStream());
                    SchoolLogoImage.Source = bitmap;
                    SchoolLogoImage.Visibility = Visibility.Visible;
                    LogoFallbackBorder.Visibility = Visibility.Collapsed;
                    return;
                }
                catch
                {
                    // fall through to the placeholder on decode failure
                }
            }
            SchoolLogoImage.Visibility = Visibility.Collapsed;
            LogoFallbackBorder.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Syncs the page header and the highlighted sidebar button when a page
        /// navigates via NavigationService.NavigateToShellPage (outside of a
        /// sidebar button click), so the shell chrome always reflects the current page.
        /// </summary>
        private void OnExternalShellNavigated(string tag)
        {
            if (!Routes.ContainsKey(tag)) return;

            // ── ROLE-BASED ROUTE GUARD (dormant during development) ──────
            // Uncomment when enforcing admin-only page access:
            // if (AdminOnlyRoutes.Contains(tag) && !_vm.IsAdministrator)
            //     return;

            if (PageMeta.TryGetValue(tag, out var meta))
            {
                PageTitleText.Text = meta.Title;
                PageSubtitleText.Text = meta.Subtitle;
            }

            var btn = FindNavButtonByTag(tag);
            if (btn != null) SetActiveButton(btn);
        }

        private Button? FindNavButtonByTag(string tag) =>
            FindVisual<Button>(this, b => b.Tag is string t && t == tag);

        private static T? FindVisual<T>(DependencyObject parent, Func<T, bool> predicate) where T : DependencyObject
        {
            int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typed && predicate(typed)) return typed;
                var found = FindVisual(child, predicate);
                if (found != null) return found;
            }
            return null;
        }

        private void NavItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag) return;
            NavigateTo(tag, btn);
        }

        private static readonly HashSet<string> AdminOnlyRoutes = new()
        {
            "Budget", "Promotion"
        };

        private async void NavigateTo(string tag, Button btn)
        {
            if (!Routes.TryGetValue(tag, out var pageType)) return;

            // ── ROLE-BASED ROUTE GUARD (dormant during development) ──────
            // Uncomment when enforcing admin-only page access:
            // if (AdminOnlyRoutes.Contains(tag) && !_vm.IsAdministrator)
            // {
            //     try
            //     {
            //         var dlg = new ContentDialog
            //         {
            //             Title = "Access Restricted",
            //             Content = "This page is restricted to administrators. Please sign in with an admin account to access it.",
            //             CloseButtonText = "OK",
            //             XamlRoot = this.XamlRoot
            //         };
            //         await dlg.ShowAsync();
            //     }
            //     catch { }
            //     return;
            // }

            // Update header
            if (PageMeta.TryGetValue(tag, out var meta))
            {
                PageTitleText.Text = meta.Title;
                PageSubtitleText.Text = meta.Subtitle;
            }

            // Active state: highlight active button
            SetActiveButton(btn);

            try
            {
                ContentFrame.Navigate(pageType);
            }
            catch (System.Exception ex)
            {
                // Surface navigation errors so they are visible during debugging/runtime
                try
                {
                    var dlg = new ContentDialog
                    {
                        Title = "Navigation error",
                        Content = ex.ToString(),
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dlg.ShowAsync();
                }
                catch { }
            }
        }

        private Brush GetThemeBrush(string resourceKey)
        {
            try
            {
                if (Application.Current is Application app)
                {
                    var themeKey = app.RequestedTheme == ApplicationTheme.Dark ? "Dark" : "Light";
                    if (app.Resources.ThemeDictionaries.TryGetValue(themeKey, out var dictObj) &&
                        dictObj is ResourceDictionary themeDict &&
                        themeDict.TryGetValue(resourceKey, out var brushObj) &&
                        brushObj is Brush brush)
                    {
                        return brush;
                    }
                }
            }
            catch { }

            return new SolidColorBrush(Colors.Transparent);
        }

        private void SetActiveButton(Button active)
        {
            // Reset previous
            if (_activeNavButton != null)
            {
                _activeNavButton.Background = new SolidColorBrush(Colors.Transparent);
                _activeNavButton.Foreground = GetThemeBrush("TextOnDarkBrush");
            }

            // Set new active: slightly gray background highlight to mark the current tab
            active.Background = GetThemeBrush("SidebarActiveBrush");
            active.Foreground = GetThemeBrush("TextOnDarkBrush");
            _activeNavButton = active;
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            SessionService.Instance.SignOut();
            NavigationService.Instance.Navigate(typeof(LoginView));
        }

        private void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (ThemeToggle.IsOn)
            {
                ThemeService.SetTheme(ApplicationTheme.Dark);
            }
            else
            {
                ThemeService.SetTheme(ApplicationTheme.Light);
            }
        }

        // Simple search result model used by the AutoSuggestBox
        private class SearchResult
        {
            public string Title { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // e.g., "Student" or "Class"
            public object? Payload { get; set; }
        }

        private async void TopSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
            var q = sender.Text?.Trim() ?? string.Empty;
            if (q.Length < 2)
            {
                sender.ItemsSource = null;
                return;
            }

            try
            {
                var ds = AppServices.DataService;
                var results = new List<SearchResult>();
                if (ds != null)
                {
                    var students = await ds.GetAllStudentsAsync();
                    foreach (var s in students.Where(s => s.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).Take(8))
                        results.Add(new SearchResult { Title = s, Type = "Student", Payload = s });

                    var classes = await ds.GetClassesAsync();
                    foreach (var c in classes.Where(c => c.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).Take(8))
                        results.Add(new SearchResult { Title = c.Name, Type = "Class", Payload = c });

                    var teachers = await ds.GetTeachersAsync();
                    foreach (var t in teachers.Where(t => t.FullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).Take(8))
                        results.Add(new SearchResult { Title = t.FullName, Type = "Teacher", Payload = t });
                }

                sender.ItemsSource = results;
            }
            catch
            {
                sender.ItemsSource = null;
            }
        }

        private async void TopSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is not SearchResult r) return;

            try
            {
                var dlg = new ContentDialog
                {
                    Title = r.Title,
                    Content = r.Type,
                    PrimaryButtonText = "Open",
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };

                var res = await dlg.ShowAsync();
                if (res == ContentDialogResult.Primary)
                {
                    // Navigate inside the shell content frame so the sidebar persists
                    var tag = r.Type switch
                    {
                        "Student" => "Students",
                        "Class"   => "Classes",
                        "Teacher" => "Teachers",
                        _         => string.Empty
                    };
                    if (!string.IsNullOrEmpty(tag))
                        NavigationService.Instance.NavigateToShellPage(tag);
                }
            }
            catch { }
        }
    }
}