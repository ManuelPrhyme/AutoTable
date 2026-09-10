using AutoTable.Data;
using AutoTable.Data.Entities;
using AutoTable.Models;
using AutoTable.Services;
using AutoTable.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.Storage.Pickers;

namespace AutoTable.Views
{
    public sealed partial class SchoolSettingsView : Page
    {
        public SchoolSettingsViewModel ViewModel { get; } = new();
        private readonly ObservableCollection<InviteCodeDisplayItem> _inviteCodes = new();
        private readonly ObservableCollection<ActiveAccountDisplayItem> _activeAccounts = new();
        private readonly ObservableCollection<AccessPageOption> _accessPageOptions = new();

        public SchoolSettingsView()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            InviteCodesList.ItemsSource = _inviteCodes;
            ActiveAccountsList.ItemsSource = _activeAccounts;

            // Hide invite code section for non-admins
            if (!SessionService.Instance.IsAdministrator)
            {
                InviteCodeCard.Visibility = Visibility.Collapsed;
                InviteCodeListCard.Visibility = Visibility.Collapsed;
                ActiveAccountsCard.Visibility = Visibility.Collapsed;
            }
            else
            {
                _ = LoadInviteCodesAsync();
                _ = LoadActiveAccountsAsync();
            }

            // Instance ID (truncated, full address in tooltip) + Sepolia balance
            var instanceId = AppServices.Key.InstanceAddress;
            if (string.IsNullOrEmpty(instanceId))
            {
                InstanceIdText.Text = "Not set";
                CopyInstanceIdButton.IsEnabled = false;
                InstanceBalanceText.Text = "";
            }
            else
            {
                InstanceIdText.Text = FormatInstanceAddress(instanceId);
                ToolTipService.SetToolTip(InstanceIdText, instanceId);
                CopyInstanceIdButton.Tag = instanceId;
                _ = LoadInstanceBalanceAsync();
            }

            // Populate the access page checkboxes (non-admin-only sidebar pages)
            var options = new List<AccessPageOption>
            {
                new() { DisplayName = "Dashboard", RouteTag = "Dashboard" },
                new() { DisplayName = "Assessments", RouteTag = "Assessments" },
                new() { DisplayName = "Marks Entry", RouteTag = "MarksEntry" },
                new() { DisplayName = "Gradebook", RouteTag = "Gradebook" },
                new() { DisplayName = "Student Performance", RouteTag = "StudentPerformance" },
                new() { DisplayName = "Analytics", RouteTag = "Analytics" },
                new() { DisplayName = "Report Cards", RouteTag = "ReportCards" },
                new() { DisplayName = "Students", RouteTag = "Students" },
                new() { DisplayName = "Teachers", RouteTag = "Teachers" },
                new() { DisplayName = "Classes Management", RouteTag = "Classes" },
                new() { DisplayName = "Fin. Dashboard", RouteTag = "FinDashboard" },
                new() { DisplayName = "Fee Collection", RouteTag = "FeeCollection" },
            };
            foreach (var opt in options)
                _accessPageOptions.Add(opt);

            // Populate the grid of checkboxes for access pages (3 columns)
            AccessPagesGrid.Children.Clear();
            int idx = 0;
            foreach (var opt in _accessPageOptions)
            {
                var cb = new CheckBox
                {
                    Content = opt.DisplayName,
                    IsEnabled = opt.IsEnabled,
                    IsChecked = opt.IsSelected,
                    Margin = new Thickness(0, 4, 0, 4),
                    Tag = opt
                };
                cb.Checked += PageCheckBox_Changed;
                cb.Unchecked += PageCheckBox_Changed;

                int row = idx / 3;
                int col = idx % 3;
                Grid.SetRow(cb, row);
                Grid.SetColumn(cb, col);
                AccessPagesGrid.Children.Add(cb);
                idx++;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SchoolSettingsViewModel.LogoBytes))
            {
                RefreshLogoPreview();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveCommand.Execute(null);
        }

        private async void PickLogo_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".gif");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var bytes = await File.ReadAllBytesAsync(file.Path);
                ViewModel.LogoBytes = bytes;
            }
        }

        private void ClearLogo_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LogoBytes = null;
        }

        private void RefreshLogoPreview()
        {
            if (ViewModel.LogoBytes != null && ViewModel.LogoBytes.Length > 0)
            {
                var bitmap = new BitmapImage();
                using var ms = new MemoryStream(ViewModel.LogoBytes);
                bitmap.SetSource(ms.AsRandomAccessStream());
                LogoPreview.Source = bitmap;
            }
            else
            {
                LogoPreview.Source = null;
            }
        }

                private async void GenerateInviteCode_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = SessionService.Instance.CurrentUser;
            if (currentUser == null) return;

            GenerateCodeButton.IsEnabled = false;
            try
            {
                var code = GenerateRandomCode();
                var label = InviteLabelBox.Text?.Trim();

                // Build the comma-selected list of allowed pages from checked boxes.
                // If all or none are selected, treat as full access (null).
                var selected = _accessPageOptions.Where(p => p.IsSelected).Select(p => p.RouteTag).ToList();
                string? allowedPages;
                if (selected.Count == 0 || selected.Count == _accessPageOptions.Count)
                    allowedPages = null; // full access
                else
                    allowedPages = string.Join(",", selected);

                var options = GetDbOptions();
                if (options == null) return;

                using var db = new AppDbContext(options);
                var invite = new InviteCodeEntity
                {
                    Code = code,
                    Role = "DataEntrant",
                    Label = string.IsNullOrWhiteSpace(label) ? null : label,
                    AllowedPages = string.IsNullOrWhiteSpace(allowedPages) ? null : allowedPages,
                    IsUsed = false,
                    CreatedByUserId = currentUser.UserId ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                };
                db.InviteCodes.Add(invite);
                await db.SaveChangesAsync();

                _ = AppServices.Audit.LogAsync("UserManagement", "GenerateInviteCode", "InviteCode",
                    invite.Id.ToString(), string.IsNullOrWhiteSpace(label) ? "Unlabelled code" : label,
                    $"Invite code generated; allowed pages: {allowedPages ?? "full access (all pages)"}.",
                    isSuccess: true);

                GeneratedCodeText.Text = code;
                GeneratedCodeBorder.Visibility = Visibility.Visible;

                // Refresh the list
                await LoadInviteCodesAsync();
            }
            catch (Exception ex)
            {
                GeneratedCodeText.Text = "Error: " + ex.Message;
                GeneratedCodeBorder.Visibility = Visibility.Visible;
            }
            finally
            {
                GenerateCodeButton.IsEnabled = true;
            }
        }

        private void CopyInviteCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string code) return;
                        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(code);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            if (btn.Content is TextBlock tb)
                tb.Text = "✓";
        }

        private async void RevokeInviteCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int inviteId) return;
            if (!SessionService.Instance.IsAdministrator) return;

            var dialog = new ContentDialog
            {
                Title = "Revoke Invite Code",
                Content = "Are you sure you want to revoke this invite code? It will no longer be usable for registration.",
                PrimaryButtonText = "Revoke",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                var options = GetDbOptions();
                if (options == null) return;

                using var db = new AppDbContext(options);
                var invite = await db.InviteCodes.FindAsync(inviteId);
                if (invite != null)
                {
                    // Mark as used (which effectively revokes it — it can't be used again)
                    invite.IsUsed = true;
                    await db.SaveChangesAsync();
                    await LoadInviteCodesAsync();
                }
            }
            catch { }
        }

        // ── Load Codes ──────────────────────────────────────────

        private async System.Threading.Tasks.Task LoadInviteCodesAsync()
        {
            try
            {
                var options = GetDbOptions();
                if (options == null) return;

                using var db = new AppDbContext(options);
                var codes = await db.InviteCodes
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                _inviteCodes.Clear();
                foreach (var c in codes)
                {
                    _inviteCodes.Add(new InviteCodeDisplayItem
                    {
                        Id = c.Id,
                        Code = c.Code,
                        Label = c.Label ?? "(no label)",
                        StatusText = c.IsUsed ? "Used" :
                                     (c.ExpiresAt.HasValue && c.ExpiresAt.Value < DateTime.UtcNow ? "Expired" : "Active"),
                        RevokeVisibility = (!c.IsUsed && (!c.ExpiresAt.HasValue || c.ExpiresAt.Value >= DateTime.UtcNow))
                            ? Visibility.Visible : Visibility.Collapsed
                    });
                }

                NoCodesText.Visibility = _inviteCodes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                InviteCodesList.Visibility = _inviteCodes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        // ── Load Active Accounts ────────────────────────────────────

        private async System.Threading.Tasks.Task LoadActiveAccountsAsync()
        {
            try
            {
                var options = GetDbOptions();
                if (options == null) return;

                using var db = new AppDbContext(options);
                var users = await db.Users
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                var currentUserId = SessionService.Instance.CurrentUser?.UserId;

                _activeAccounts.Clear();
                foreach (var u in users)
                {
                    var isAdmin = string.Equals(u.Role, "Administrator", StringComparison.OrdinalIgnoreCase);
                    _activeAccounts.Add(new ActiveAccountDisplayItem
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        Email = u.Email ?? "(no email)",
                        Role = u.Role ?? "DataEntrant",
                        CreatedAtDisplay = u.CreatedAt.ToString("dd MMM yyyy"),
                        // Can't enhance or revoke yourself, and can't modify other admins
                        CanEnhanceVisibility = (!isAdmin && u.Id != currentUserId) ? Visibility.Visible : Visibility.Collapsed,
                        CanRevokeVisibility = (!isAdmin && u.Id != currentUserId) ? Visibility.Visible : Visibility.Collapsed,
                        // Admins are indelible — hide Remove for every admin account (not just self)
                        CanRemoveVisibility = (!isAdmin && u.Id != currentUserId) ? Visibility.Visible : Visibility.Collapsed
                    });
                }

                // Populate the ResetUserCombo so admins can pick a user to generate credential reset codes
                ResetUserCombo.Items.Clear();
                foreach (var u in users)
                {
                    var item = new ComboBoxItem { Content = u.FullName, Tag = u.Id };
                    ResetUserCombo.Items.Add(item);
                }
            }
            catch { }
        }

        // ── Active Account Actions ───────────────────────────────────

        private async void PromoteAccess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int userId) return;

            var account = _activeAccounts.FirstOrDefault(a => a.Id == userId);
            if (account == null) return;

            var options = GetDbOptions();
            if (options == null) return;

            using var db = new AppDbContext(options);
            var user = await db.Users.FindAsync(userId);
            if (user == null) return;

            var currentAllowedPages = string.IsNullOrEmpty(user.AllowedPages)
                ? new List<string>()
                : user.AllowedPages.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var allPages = new List<AccessPageOption>
            {
                new() { DisplayName = "Dashboard", RouteTag = "Dashboard" },
                new() { DisplayName = "Assessments", RouteTag = "Assessments" },
                new() { DisplayName = "Marks Entry", RouteTag = "MarksEntry" },
                new() { DisplayName = "Gradebook", RouteTag = "Gradebook" },
                new() { DisplayName = "Student Performance", RouteTag = "StudentPerformance" },
                new() { DisplayName = "Analytics", RouteTag = "Analytics" },
                new() { DisplayName = "Report Cards", RouteTag = "ReportCards" },
                new() { DisplayName = "Students", RouteTag = "Students" },
                new() { DisplayName = "Teachers", RouteTag = "Teachers" },
                new() { DisplayName = "Classes Management", RouteTag = "Classes" },
                new() { DisplayName = "Fin. Dashboard", RouteTag = "FinDashboard" },
                new() { DisplayName = "Fee Collection", RouteTag = "FeeCollection" },
            };

            var unselectedPages = allPages
                .Where(p => !currentAllowedPages.Contains(p.RouteTag))
                .ToList();

            if (unselectedPages.Count == 0)
            {
                var noPagesDialog = new ContentDialog
                {
                    Title = $"Promote Access — {account.FullName}",
                    Content = "This user already has access to all available pages.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await noPagesDialog.ShowAsync();
                return;
            }

            var dialog = new ContentDialog
            {
                Title = $"Promote Access — {account.FullName}",
                PrimaryButtonText = "Grant Access",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var checkboxes = new System.Collections.Generic.List<(CheckBox CheckBox, string RouteTag)>();
            var panel = new StackPanel { Spacing = 8 };

            var selectAllCheck = new CheckBox { Content = "Select All", IsThreeState = true, IsChecked = false };
            selectAllCheck.Checked += (_, _) => { foreach (var (cb, _) in checkboxes) cb.IsChecked = true; };
            selectAllCheck.Unchecked += (_, _) => { foreach (var (cb, _) in checkboxes) cb.IsChecked = false; };
            panel.Children.Add(selectAllCheck);

            var grid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var row = 0;
            var col = 0;

            foreach (var page in unselectedPages)
            {
                var cb = new CheckBox { Content = page.DisplayName, IsChecked = false };
                cb.Checked += (_, _) => UpdateSelectAllState(checkboxes, selectAllCheck);
                cb.Unchecked += (_, _) => UpdateSelectAllState(checkboxes, selectAllCheck);
                checkboxes.Add((cb, page.RouteTag));
                Grid.SetColumn(cb, col);
                Grid.SetRow(cb, row);
                grid.Children.Add(cb);
                col++;
                if (col >= 3) { col = 0; row++; }
            }

            for (int i = 0; i < row + 1; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            panel.Children.Add(grid);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                var selectedPages = checkboxes.Where(c => c.CheckBox.IsChecked == true).Select(c => c.RouteTag).ToList();
                if (selectedPages.Count == 0) return;

                var updatedPages = new List<string>(currentAllowedPages);
                foreach (var page in selectedPages)
                    if (!updatedPages.Contains(page)) updatedPages.Add(page);

                user.AllowedPages = string.Join(",", updatedPages);
                await db.SaveChangesAsync();

                _ = AppServices.Audit.LogAsync("UserManagement", "Promote", "User",
                    user.Id.ToString(), user.FullName,
                    $"Granted access to: {string.Join(", ", selectedPages)}.", isSuccess: true);

                await LoadActiveAccountsAsync();
            }
            catch { }
        }

        private async void DemoteAccess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int userId) return;

            var account = _activeAccounts.FirstOrDefault(a => a.Id == userId);
            if (account == null) return;

            var options = GetDbOptions();
            if (options == null) return;

            using var db = new AppDbContext(options);
            var user = await db.Users.FindAsync(userId);
            if (user == null) return;

            var currentAllowedPages = string.IsNullOrEmpty(user.AllowedPages)
                ? new List<string>()
                : user.AllowedPages.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (currentAllowedPages.Count == 0)
            {
                var noPagesDialog = new ContentDialog
                {
                    Title = $"Demote Access — {account.FullName}",
                    Content = "This user currently has no page access to revoke.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await noPagesDialog.ShowAsync();
                return;
            }

            var dialog = new ContentDialog
            {
                Title = $"Demote Access — {account.FullName}",
                PrimaryButtonText = "Revoke Selected",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var checkboxes = new System.Collections.Generic.List<(CheckBox CheckBox, string RouteTag)>();
            var panel = new StackPanel { Spacing = 8 };

            var instructionText = new TextBlock
            {
                Text = $"Uncheck pages to revoke access for {account.FullName}:",
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = new Thickness(0, 0, 0, 4)
            };
            panel.Children.Add(instructionText);

            // 3-column grid for the checkboxes (pages user currently has)
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var allPages = new List<(string DisplayName, string RouteTag)>
            {
                ("Dashboard", "Dashboard"),
                ("Assessments", "Assessments"),
                ("Marks Entry", "MarksEntry"),
                ("Gradebook", "Gradebook"),
                ("Student Performance", "StudentPerformance"),
                ("Analytics", "Analytics"),
                ("Report Cards", "ReportCards"),
                ("Students", "Students"),
                ("Teachers", "Teachers"),
                ("Classes Management", "Classes"),
                ("Fin. Dashboard", "FinDashboard"),
                ("Fee Collection", "FeeCollection"),
            };

            var row = 0;
            var col = 0;
            // Only pages the user currently has access to belong in the Demote modal —
            // unchecking one revokes that specific page.
            foreach (var (displayName, routeTag) in allPages.Where(p => currentAllowedPages.Contains(p.RouteTag)))
            {
                var cb = new CheckBox { Content = displayName, IsChecked = true };
                checkboxes.Add((cb, routeTag));

                Grid.SetColumn(cb, col);
                Grid.SetRow(cb, row);
                grid.Children.Add(cb);

                col++;
                if (col >= 3) { col = 0; row++; }
            }

            for (int i = 0; i < row + 1; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            panel.Children.Add(grid);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                // Keep only pages that are still checked
                var remainingPages = checkboxes.Where(c => c.CheckBox.IsChecked == true).Select(c => c.RouteTag).ToList();

                if (remainingPages.Count == 0)
                {
                    // No pages left — lock out the user
                    user.AllowedPages = "__NONE__";
                }
                else
                {
                    user.AllowedPages = string.Join(",", remainingPages);
                }
                await db.SaveChangesAsync();

                _ = AppServices.Audit.LogAsync("UserManagement", "Demote", "User",
                    user.Id.ToString(), user.FullName,
                    remainingPages.Count == 0
                        ? "All page access revoked (account locked out)."
                        : $"Access reduced to: {string.Join(", ", remainingPages)}.",
                    isSuccess: true);

                await LoadActiveAccountsAsync();
            }
            catch { }
        }

        private static void UpdateSelectAllState(System.Collections.Generic.List<(CheckBox CheckBox, string RouteTag)> checkboxes, CheckBox selectAllCheck)
        {
            int checkedCount = checkboxes.Count(c => c.CheckBox.IsChecked == true);
            if (checkedCount == 0) selectAllCheck.IsChecked = false;
            else if (checkedCount == checkboxes.Count) selectAllCheck.IsChecked = true;
            else selectAllCheck.IsChecked = null;
        }

        private async void RevokeAccess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int userId) return;

            var account = _activeAccounts.FirstOrDefault(a => a.Id == userId);
            if (account == null) return;

            var dialog = new ContentDialog
            {
                Title = $"Revoke Access — {account.FullName}",
                Content = $"Are you sure you want to revoke all access for '{account.FullName}'? " +
                          "They will no longer be able to sign in to the application.",
                PrimaryButtonText = "Revoke",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                var options = GetDbOptions();
                if (options == null) return;
                using var db = new AppDbContext(options);
                var user = await db.Users.FindAsync(userId);
                if (user == null) return;

                // Downgrade to DataEntrant with no allowed pages (effectively locked out)
                user.Role = "DataEntrant";
                user.AllowedPages = "__NONE__"; // no page grants access
                await db.SaveChangesAsync();

                _ = AppServices.Audit.LogAsync("UserManagement", "RevokeAccess", "User",
                    user.Id.ToString(), user.FullName,
                    "All access revoked — account locked out.", isSuccess: true);

                await LoadActiveAccountsAsync();
            }
            catch { }
        }

        private async void RemoveAccount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int userId) return;

            var account = _activeAccounts.FirstOrDefault(a => a.Id == userId);
            if (account == null) return;

            var dialog = new ContentDialog
            {
                Title = $"Remove Account — {account.FullName}",
                Content = $"Are you sure you want to permanently delete the account for '{account.FullName}'? " +
                          "This action cannot be undone. Existing marks and fee records entered by this user will be preserved.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                var options = GetDbOptions();
                if (options == null) return;
                using var db = new AppDbContext(options);
                var user = await db.Users.FindAsync(userId);
                if (user == null) return;

                db.Users.Remove(user);
                await db.SaveChangesAsync();

                _ = AppServices.Audit.LogAsync("UserManagement", "Delete", "User",
                    user.Id.ToString(), user.FullName,
                    "User account permanently deleted.", isSuccess: true);

                await LoadActiveAccountsAsync();
            }
            catch (Exception ex)
            {
                var errDialog = new ContentDialog
                {
                    Title = "Cannot Remove Account",
                    Content = $"The account could not be removed: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errDialog.ShowAsync();
            }
        }

        // ── Access Page Checkbox Events ─────────────────────────────

        private void SelectAllPagesCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox chk) return;
            bool check = chk.IsChecked == true;

            // Sync both the model AND the visual checkboxes so the grid stays in
            // sync with the "Select All (Full Access)" toggle. The per-page
            // Checked/Unchecked handlers keep AccessPageOption.IsSelected in sync.
            foreach (var opt in _accessPageOptions)
                opt.IsSelected = check;

            foreach (var child in AccessPagesGrid.Children)
            {
                if (child is CheckBox cb && cb.Tag is AccessPageOption pageOpt)
                {
                    if (cb.IsChecked != check)
                        cb.IsChecked = check;
                    // Belt-and-braces: the event may not fire if value was already set;
                    // force the model property to match regardless.
                    pageOpt.IsSelected = check;
                }
            }
        }

        private void PageCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Sync the individual checkbox's IsChecked back to the AccessPageOption.IsSelected
            // property so that GenerateInviteCode_Click reads the correct selected pages.
            if (sender is CheckBox cb && cb.Tag is AccessPageOption opt)
            {
                opt.IsSelected = cb.IsChecked == true;
            }

            // Sync the "Select All" checkbox state based on individual items.
            int selectedCount = _accessPageOptions.Count(p => p.IsSelected);
            if (selectedCount == 0)
                SelectAllPagesCheck.IsChecked = false;
            else if (selectedCount == _accessPageOptions.Count)
                SelectAllPagesCheck.IsChecked = true;
            else
                SelectAllPagesCheck.IsChecked = null; // indeterminate
        }

        private async void ResetUserCombo_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ResetUserCombo.SelectedItem is not ComboBoxItem cb || cb.Tag is not int userId)
                return;

            try
            {
                var options = GetDbOptions();
                if (options == null) return;
                using var db = new AppDbContext(options);
                var user = await db.Users.FindAsync(userId);
                if (user == null)
                {
                    CurrentResetCodeText.Visibility = Visibility.Collapsed;
                    CopyResetCodeButton.Visibility = Visibility.Collapsed;
                    return;
                }

                if (string.IsNullOrEmpty(user.CredentialResetCode))
                {
                    CurrentResetCodeText.Visibility = Visibility.Collapsed;
                    CopyResetCodeButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    CurrentResetCodeRun.Text = user.CredentialResetCode;
                    CurrentResetCodeText.Visibility = Visibility.Visible;
                    CopyResetCodeButton.Visibility = Visibility.Visible;
                }
            }
            catch { }
        }

        private async void GenerateResetCode_Click(object sender, RoutedEventArgs e)
        {
            if (ResetUserCombo.SelectedItem is not ComboBoxItem cb || cb.Tag is not int userId) return;

            GenerateResetCodeButton.IsEnabled = false;
            try
            {
                var code = GenerateRandomCode();
                var options = GetDbOptions();
                if (options == null) return;
                using var db = new AppDbContext(options);
                var user = await db.Users.FindAsync(userId);
                if (user == null) return;
                user.CredentialResetCode = code;
                await db.SaveChangesAsync();

                _ = AppServices.Audit.LogAsync("UserManagement", "GenerateResetCode", "User",
                    user.Id.ToString(), user.FullName,
                    "A new credential reset code was generated for this account.", isSuccess: true);

                NewResetCodeText.Text = code;
                NewResetCodeBorder.Visibility = Visibility.Visible;
                CurrentResetCodeRun.Text = code;
                CurrentResetCodeText.Visibility = Visibility.Visible;
                CopyResetCodeButton.Visibility = Visibility.Visible;
            }
            catch { }
            finally { GenerateResetCodeButton.IsEnabled = true; }
        }

        private void CopyResetCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            try
            {
                var code = NewResetCodeBorder.Visibility == Visibility.Visible
                    ? NewResetCodeText.Text
                    : CurrentResetCodeRun.Text;
                var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dp.SetText(code ?? string.Empty);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);

                if (btn.Content is TextBlock tb)
                    tb.Text = "✓";
            }
            catch { }
        }

        private void CopyInstanceId_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string address) return;
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(address);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            if (btn.Content is TextBlock tb)
                tb.Text = "✓";
        }

        /// <summary>Formats a full 0x… address as 0x0222…3423 (first 4 + last 4 hex chars).</summary>
        private static string FormatInstanceAddress(string address)
            => address.Length <= 12 ? address : $"{address[..6]}...{address[^4..]}";

        /// <summary>Fetches the instance's Sepolia balance (GetBalanceAsync already returns ETH, not Wei).</summary>
        private async System.Threading.Tasks.Task LoadInstanceBalanceAsync()
        {
            try
            {
                var balance = await AppServices.License.GetBalanceAsync();
                InstanceBalanceText.Text = $"{balance:F6} ETH";
            }
            catch
            {
                InstanceBalanceText.Text = "Balance unavailable";
            }
        }


        // ── Helpers ─────────────────────────────────────────────

        private static DbContextOptions<AppDbContext>? GetDbOptions()
        {
            var connStr = AppServices.AuthConnectionString;
            if (string.IsNullOrEmpty(connStr)) return null;
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connStr)
                .AddInterceptors(new AppDbContext.ForeignKeyInterceptor())
                .Options;
        }

        private static string GenerateRandomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I/1/O/0
            var random = new Random();
            var part1 = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
            var part2 = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
            return $"{part1}-{part2}";
        }
    }

    /// <summary>
    /// Display-friendly representation of an invite code for the ListView.
    /// </summary>
    public class InviteCodeDisplayItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public Visibility RevokeVisibility { get; set; }
    }

    /// <summary>
    /// Display-friendly representation of a registered user account for the Active Accounts ListView.
    /// </summary>
    public class ActiveAccountDisplayItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string CreatedAtDisplay { get; set; } = string.Empty;
        public Visibility CanEnhanceVisibility { get; set; }
        public Visibility CanRevokeVisibility { get; set; }
        public Visibility CanRemoveVisibility { get; set; }

        public string RoleLabel => string.Equals(Role, "Administrator", StringComparison.OrdinalIgnoreCase)
            ? "Admin"
            : "Staff";

        public Brush RoleBadgeBackground => string.Equals(Role, "Administrator", StringComparison.OrdinalIgnoreCase)
            ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 220, 230, 255)) // light blue
            : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 230, 230, 230)); // light gray

        public Brush RoleBadgeForeground => string.Equals(Role, "Administrator", StringComparison.OrdinalIgnoreCase)
            ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 40, 80, 160)) // dark blue
            : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 100, 100)); // dark gray
    }

    /// <summary>
    /// Represents a selectable page in the invite-code access-level checkbox list.
    /// </summary>
    public class AccessPageOption : INotifyPropertyChanged
    {
        public string DisplayName { get; set; } = string.Empty;
        public string RouteTag { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
