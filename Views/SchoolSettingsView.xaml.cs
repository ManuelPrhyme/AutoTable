using AutoTable.Data;
using AutoTable.Data.Entities;
using AutoTable.Models;
using AutoTable.Services;
using AutoTable.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

        public SchoolSettingsView()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            InviteCodesList.ItemsSource = _inviteCodes;

            // Hide invite code section for non-admins
            if (!SessionService.Instance.IsAdministrator)
            {
                InviteCodeCard.Visibility = Visibility.Collapsed;
                InviteCodeListCard.Visibility = Visibility.Collapsed;
            }
            else
            {
                _ = LoadInviteCodesAsync();
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
                var allowedPages = (InviteAccessCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();

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
}
