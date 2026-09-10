using AutoTable.Services;
using AutoTable.Models;
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
            ["ReportCards"]        = ("Report Cards", "Generate and print student report cards."),
            ["Teachers"]           = ("Teachers", "Register and manage teachers."),
            ["Students"]           = ("Students", "Manage student records, LIN identifiers, and termination."),
            ["Classes"]            = ("Classes & Subjects", "Manage classes, subjects, and subject assignments."),
            ["Promotion"]          = ("Promotion / Repeat", "End-of-year (Term 3) promote or repeat decisions per student."),
            ["AuditLog"]           = ("Termination / Audit Log", "View termination history and anonymization records."),
            ["TermManagement"]     = ("Term Management", "Manage academic terms, set term fees, and activate/deactivate terms."),
            ["FinDashboard"]       = ("Financial Dashboard", "Overview of fee collection, budget, and expenditure."),
            ["FeeCollection"]      = ("Fee Collection", "Track and manage student fee payments."),
            ["Budget"]             = ("Budget & Expenditure", "School budget planning and expenditure tracking."),

        };

        private static readonly Dictionary<string, System.Type> Routes = new()
        {
            ["Dashboard"]          = typeof(DashboardView),
            ["Assessments"]        = typeof(AssessmentsView),
            ["MarksEntry"]         = typeof(MarksEntryView),
            ["Gradebook"]          = typeof(GradebookView),
            ["StudentPerformance"] = typeof(StudentPerformanceView),
            ["Analytics"]          = typeof(AnalyticsView),
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

            ["SchoolSettings"]     = typeof(SchoolSettingsView),
        };

        /// <summary>
        /// Landing-page preference for restricted data entrants: the shell opens
        /// the first of these routes their AllowedPages grants (Dashboard when
        /// unrestricted), instead of showing an "Access Restricted" dialog and
        /// a blank content frame.
        /// </summary>
        private static readonly string[] PreferredStartTags =
        {
            "Dashboard", "MarksEntry", "Gradebook", "Assessments", "Students", "Teachers",
            "ReportCards", "FeeCollection", "FinDashboard", "Classes", "StudentPerformance",
            "Analytics"
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

            // ── LOAD SCHOOL BRANDING ────────────────────────────────────
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

            // ── OFFLINE LICENSE VALIDATION (every startup, no internet) ──
            try
            {
                var licenseCheck = AppServices.LicenseManager.Validate();
                if (licenseCheck.NotActivated)
                {
                    // No local license yet → prompt for activation (needs internet once).
                    await TryShowLicenseActivationAsync();
                }
                else if (licenseCheck.SignatureInvalid ||
                         licenseCheck.CounterRegression ||
                         licenseCheck.ClockRollback)
                {
                    await ShowLicenseBlockedAsync(licenseCheck.Message);
                }
                else if (licenseCheck.IsGrace)
                {
                    AppServices.Toasts.Show("License Expired — Grace Period",
                        licenseCheck.Message + " You can view data but editing is disabled until you renew.");
                }
            }
            catch { /* licensing must never block the shell from loading */ }

            // ── ROLE-BASED SIDEBAR GATING ─────────────────────────────
            var isAdmin = _vm.IsAdministrator;
            var allowedPages = SessionService.Instance.CurrentUser?.AllowedPages;
            var hasPageRestrictions = !isAdmin && !string.IsNullOrWhiteSpace(allowedPages);
            var allowedSet = hasPageRestrictions
                ? new HashSet<string>(allowedPages!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                : null;

            // Admin-only pages: hidden for non-admins
            NavTermManagement.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            NavPromotion.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            NavAuditLog.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            // Pages restricted by invite-code AllowedPages
            if (hasPageRestrictions && allowedSet != null)
            {
                NavDashboard.Visibility = allowedSet.Contains("Dashboard") ? Visibility.Visible : Visibility.Collapsed;
                NavAssessments.Visibility = allowedSet.Contains("Assessments") ? Visibility.Visible : Visibility.Collapsed;
                NavMarksEntry.Visibility = allowedSet.Contains("MarksEntry") ? Visibility.Visible : Visibility.Collapsed;
                NavGradebook.Visibility = allowedSet.Contains("Gradebook") ? Visibility.Visible : Visibility.Collapsed;
                NavStudentPerformance.Visibility = allowedSet.Contains("StudentPerformance") ? Visibility.Visible : Visibility.Collapsed;
                NavAnalytics.Visibility = allowedSet.Contains("Analytics") ? Visibility.Visible : Visibility.Collapsed;
                NavReportCards.Visibility = allowedSet.Contains("ReportCards") ? Visibility.Visible : Visibility.Collapsed;
                NavStudents.Visibility = allowedSet.Contains("Students") ? Visibility.Visible : Visibility.Collapsed;
                NavTeachers.Visibility = allowedSet.Contains("Teachers") ? Visibility.Visible : Visibility.Collapsed;
                NavClasses.Visibility = allowedSet.Contains("Classes") ? Visibility.Visible : Visibility.Collapsed;
                NavFinDashboard.Visibility = allowedSet.Contains("FinDashboard") ? Visibility.Visible : Visibility.Collapsed;
                NavFeeCollection.Visibility = allowedSet.Contains("FeeCollection") ? Visibility.Visible : Visibility.Collapsed;

                NavBudget.Visibility = allowedSet.Contains("Budget") ? Visibility.Visible : Visibility.Collapsed;
                NavAiInsights.Visibility = allowedSet.Contains("AiInsights") ? Visibility.Visible : Visibility.Collapsed;
                NavSchoolSettings.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (!isAdmin)
            {
                // No restrictions but not admin: show all non-admin pages
                NavClasses.Visibility = Visibility.Visible;
                NavBudget.Visibility = Visibility.Visible;
            }

            // ── HIDE SECTIONS WITH NO VISIBLE PAGES ─────────────────
            // If every button in a section is collapsed, hide the section header too.
            SectionPerformance.Visibility = HasAnyVisible(
                NavDashboard, NavAssessments, NavMarksEntry, NavGradebook,
                NavStudentPerformance, NavAnalytics) ? Visibility.Visible : Visibility.Collapsed;

            SectionAdmin.Visibility = HasAnyVisible(
                NavReportCards, NavStudents, NavTeachers, NavTermManagement,
                NavClasses, NavPromotion, NavAuditLog) ? Visibility.Visible : Visibility.Collapsed;

            SectionFinancials.Visibility = HasAnyVisible(
                NavFinDashboard, NavFeeCollection, NavBudget) ? Visibility.Visible : Visibility.Collapsed;

            NavigationService.Instance.InitializeShell(ContentFrame);

            // Keep header/sidebar in sync when other pages navigate the shell frame
            // (e.g. Dashboard Quick Actions, top-search suggestions).
            NavigationService.Instance.ShellNavigated -= OnExternalShellNavigated;
            NavigationService.Instance.ShellNavigated += OnExternalShellNavigated;
            Unloaded += (_, _) => NavigationService.Instance.ShellNavigated -= OnExternalShellNavigated;

            // Land restricted users on the first page their AllowedPages grant,
            // falling back to Dashboard for admins and unrestricted users.
            var startTag = "Dashboard";
            if (hasPageRestrictions && allowedSet != null)
            {
                var granted = allowedSet.Where(Routes.ContainsKey).ToList();
                startTag = PreferredStartTags.FirstOrDefault(granted.Contains) ?? granted.FirstOrDefault() ?? string.Empty;
            }
            if (!string.IsNullOrEmpty(startTag))
                NavigateTo(startTag, FindNavButtonByTag(startTag) ?? NavDashboard);

            // ── AUTO-START USER TOUR ON NEW INSTALLATION ─────────
            // The tour auto-starts when the database is empty (no classes
            // AND no terms) — indicating a fresh install. It also starts
            // if the user has never completed the tour on an existing DB.
            bool shouldStartTour = false;
            try
            {
                if (AppServices.DataService != null)
                {
                    var classes = await AppServices.DataService.GetClassesAsync();
                    var terms = await AppServices.DataService.GetTermsAsync();
                    // New installation: empty DB — always tour
                    if (classes.Count == 0 && terms.Count == 0)
                        shouldStartTour = true;
                    // Existing DB but tour never completed — still tour
                    else if (!UserTourService.Instance.HasCompletedTour)
                        shouldStartTour = true;
                }
                else if (!UserTourService.Instance.HasCompletedTour)
                {
                    shouldStartTour = true;
                }
            }
            catch { }

            // Restricted data entrants don't get the tour: its steps visit pages
            // outside their AllowedPages scope.
            var canTour = isAdmin || string.IsNullOrWhiteSpace(allowedPages);
            TakeTourBtn.Visibility = canTour ? Visibility.Visible : Visibility.Collapsed;

            if (shouldStartTour && canTour)
            {
                // Small delay so the UI has time to render before
                // we calculate spotlight positions.
                await Task.Delay(600);
                TourOverlay.StartTour(this);
                // Don't run first-launch setup here — TourOverlay_TourFinished will do it.
            }
            else
            {
                // Tour already done — run first-launch setup immediately.
                await RunFirstLaunchSetupIfNeeded();
            }
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
        /// Prompts the user to activate their license when no local license file
        /// exists (first run after setup). Requires internet once.
        /// </summary>
        private async Task TryShowLicenseActivationAsync()
        {
            // Only prompt on the dashboard landing, once.
            if (ContentFrame != null && ContentFrame.CurrentSourcePageType != null &&
                ContentFrame.CurrentSourcePageType.Name != "DashboardView")
            {
                return;
            }

            var dialog = new LicenseActivationDialog { XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
            if (dialog.Activated)
            {
                AppServices.Toasts.Show("License Active", "Your license is now active. Enjoy AutoTable!");
            }
        }

        /// <summary>
        /// Shows a blocking dialog when the license was tampered with or
        /// the hard-lock period has passed.
        /// </summary>
        private async Task ShowLicenseBlockedAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "License Issue",
                Content = new TextBlock
                {
                    Text = message + "\n\nContact your vendor to resolve this before continuing.",
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(0, 8, 0, 0)
                },
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// Syncs the page header and the highlighted sidebar button when a page
        /// navigates via NavigationService.NavigateToShellPage (outside of a
        /// sidebar button click), so the shell chrome always reflects the current page.
        /// </summary>
        private void OnExternalShellNavigated(string tag)
        {
            if (!Routes.ContainsKey(tag)) return;

            // ── ROLE-BASED ROUTE GUARD ─────────────────────────────────
            if (NavigationService.AdminOnlyRouteTags.Contains(tag) && !_vm.IsAdministrator)
                return;

            // ── INVITE-CODE PAGE RESTRICTION ───────────────────────────
            var currentUser = SessionService.Instance.CurrentUser;
            if (currentUser != null && currentUser.Role != UserRole.Administrator
                && !string.IsNullOrWhiteSpace(currentUser.AllowedPages))
            {
                var allowed = new HashSet<string>(currentUser.AllowedPages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                if (!allowed.Contains(tag))
                    return;
            }

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

        /// <summary>
        /// Returns true if any of the provided UI elements is visible.
        /// Used to determine whether a sidebar section header should be shown.
        /// </summary>
        private static bool HasAnyVisible(params UIElement[] elements)
        {
            foreach (var el in elements)
            {
                if (el.Visibility == Visibility.Visible) return true;
            }
            return false;
        }

        private void NavItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag) return;
            NavigateTo(tag, btn);
        }

        private async void NavigateTo(string tag, Button btn)
        {
            if (!Routes.TryGetValue(tag, out var pageType)) return;

            // ── ROLE-BASED ROUTE GUARD ─────────────────────────────────
            if (NavigationService.AdminOnlyRouteTags.Contains(tag) && !_vm.IsAdministrator)
            {
                try
                {
                    var dlg = new ContentDialog
                    {
                        Title = "Access Restricted",
                        Content = "This page is restricted to administrators. Please sign in with an admin account to access it.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dlg.ShowAsync();
                }
                catch { }
                return;
            }

            // ── INVITE-CODE PAGE RESTRICTION GUARD ──────────────────────
            var currentUser = SessionService.Instance.CurrentUser;
            if (currentUser != null && currentUser.Role != UserRole.Administrator
                && !string.IsNullOrWhiteSpace(currentUser.AllowedPages))
            {
                var allowed = new HashSet<string>(currentUser.AllowedPages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                if (!allowed.Contains(tag))
                {
                    try
                    {

                        var dlg = new ContentDialog
                        {
                            Title = "Access Restricted",
                            Content = "You do not have permission to access this page. Your invite code does not include access to this section.",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await dlg.ShowAsync();
                    }
                    catch { }
                    return;
                }
            }

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
                _activeNavButton.BorderBrush = new SolidColorBrush(Colors.Transparent);
                _activeNavButton.BorderThickness = new Thickness(0);
                _activeNavButton.CornerRadius = new CornerRadius(8);
            }

            // Set new active: slightly gray background highlight with white bottom border to mark the current tab
            active.Background = GetThemeBrush("SidebarActiveBrush");
            active.Foreground = GetThemeBrush("TextOnDarkBrush");
            active.BorderBrush = new SolidColorBrush(Colors.White);
            active.BorderThickness = new Thickness(0, 0, 0, 2);
            active.CornerRadius = new CornerRadius(0);
            _activeNavButton = active;
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            var cu = SessionService.Instance.CurrentUser;
            if (cu != null)
                _ = AppServices.Audit.LogAsync("Authentication", "Logout", "User",
                    cu.UserId?.ToString(), cu.FullName, "User signed out.", isSuccess: true);
            SessionService.Instance.SignOut();
            // Navigate back to the appropriate login page based on admin existence
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

        // ── USER TOUR ──────────────────────────────────────────

        private void TakeTour_Click(object sender, RoutedEventArgs e)
        {
            TourOverlay.StartTour(this);
        }

        /// <summary>
        /// Navigates to a page by tag and waits for the layout to settle.
        /// Used by the user tour overlay to navigate between pages.
        /// </summary>
        internal async Task NavigateToPageForTourAsync(string tag)
        {
            if (!Routes.TryGetValue(tag, out var pageType)) return;

            if (PageMeta.TryGetValue(tag, out var meta))
            {
                PageTitleText.Text = meta.Title;
                PageSubtitleText.Text = meta.Subtitle;
            }

            var btn = FindNavButtonByTag(tag);
            if (btn != null) SetActiveButton(btn);

            ContentFrame.Navigate(pageType);

            // Give the page time to load its visual tree
            await Task.Delay(400);
        }

        /// <summary>
        /// Returns the content frame so the tour overlay can search
        /// for named elements inside the currently loaded page.
        /// </summary>
        internal Frame GetContentFrame() => ContentFrame;

        private async void AiInsights_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new ContentDialog
                {
                    Title = "AI Insights",
                    Content = "This feature is coming soon! AI-powered recommendations and workflow automations will be available in a future update.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dlg.ShowAsync();
            }
            catch { }
        }

        private async void TourOverlay_TourFinished()
        {
            // Tour completed or dismissed — now run the first-launch setup
            // if no classes/grading systems exist yet.
            await RunFirstLaunchSetupIfNeeded();
        }

        /// <summary>
        /// Checks whether the school database is empty (no classes) and
        /// kicks off the first-launch setup sequence: grading-system
        /// creation → class creation → class teacher assignment.
        /// Safe to call multiple times — only acts when there are zero classes.
        /// </summary>
        private async Task RunFirstLaunchSetupIfNeeded()
        {
            if (AppServices.DataService == null) return;
            try
            {
                var classes = await AppServices.DataService.GetClassesAsync();
                if (classes.Count == 0)
                {
                    SessionService.Instance.ShouldAutoOpenGradingSystemCreation = true;
                    NavigateTo("Classes", NavClasses);
                }
            }
            catch { }
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