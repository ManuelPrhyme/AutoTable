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

            AccessPagesList.ItemsSource = _accessPageOptions;
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

        // ── Invite Code Copy ──────────────────────────────

        private async void CopyInviteCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string code) return;
            try
            {
                var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dp.SetText(code);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);

                // Brief visual feedback
                var originalContent = btn.Content;
                btn.Content = "\u2713";
                btn.IsEnabled = false;
                await System.Threading.Tasks.Task.Delay(1200);
                btn.Content = originalContent;
                btn.IsEnabled = true;
            }
            catch { }
        }

        // ── Invite Code Generation ──────────────────────────────

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
                        // Always show Remove, except don't let admin remove themselves
                        CanRemoveVisibility = (u.Id != currentUserId) ? Visibility.Visible : Visibility.Collapsed
                    });
                }
            }
            catch { }
        }

        // ── Active Account Actions ───────────────────────────────────

        private async void EnhanceAccess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not int userId) return;

            var account = _activeAccounts.FirstOrDefault(a => a.Id == userId);
            if (account == null) return;

            // Build a list of possible access levels
            var options = GetDbOptions();
            if (options == null) return;

            var enhanceChoices = new[]
            {
                "Promote to Administrator",
                "Full Access (all pages)",
                "Marks Entry + Gradebook",
                "Fee Collection Only",
                "Marks Entry Only"
            };

            var dialog = new ContentDialog
            {
                Title = $"Enhance Access — {account.FullName}",
                Content = "Select the new access level for this account.",
                PrimaryButtonText = "Apply",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            // Use a ComboBox inside a StackPanel for the dialog content
            var panel = new StackPanel { Spacing = 8 };
            var combo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
            foreach (var choice in enhanceChoices)
                combo.Items.Add(choice);
            combo.SelectedIndex = 1; // default to Full Access
            panel.Children.Add(combo);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                using var db = new AppDbContext(options);
                var user = await db.Users.FindAsync(userId);
                if (user == null) return;

                var selected = combo.SelectedItem?.ToString();
                if (selected == "Promote to Administrator")
                {
                    user.Role = "Administrator";
                    user.AllowedPages = null;
                }
                else if (selected == "Full Access (all pages)")
                {
                    user.Role = "DataEntrant";
                    user.AllowedPages = null;
                }
                else
                {
                    user.Role = "DataEntrant";
                    user.AllowedPages = selected switch
                    {
                        "Marks Entry + Gradebook" => "MarksEntry,Gradebook",
                        "Fee Collection Only" => "FeeCollection",
                        "Marks Entry Only" => "MarksEntry",
                        _ => null
                    };
                }

                await db.SaveChangesAsync();
                await LoadActiveAccountsAsync();
            }
            catch { }
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
            foreach (var opt in _accessPageOptions)
                opt.IsSelected = check;
        }

        private void PageCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Sync the "Select All" checkbox state based on individual items.
            int selectedCount = _accessPageOptions.Count(p => p.IsSelected);
            if (selectedCount == 0)
                SelectAllPagesCheck.IsChecked = false;
            else if (selectedCount == _accessPageOptions.Count)
                SelectAllPagesCheck.IsChecked = true;
            else
                SelectAllPagesCheck.IsChecked = null; // indeterminate
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
