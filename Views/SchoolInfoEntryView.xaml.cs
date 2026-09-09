using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using AutoTable.Data;
using AutoTable.Services;

namespace AutoTable.Views
{
    public sealed partial class SchoolInfoEntryView : Page
    {
        private bool _completed = false;

        public bool Completed => _completed;

        public SchoolInfoEntryView()
        {
            this.InitializeComponent();
        }

        private async void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            var schoolName = SchoolNameBox.Text?.Trim();
            var schoolAddress = SchoolAddressBox.Text?.Trim();
            var schoolPhone = SchoolPhoneBox.Text?.Trim();
            var adminEmail = AdminEmailBox.Text?.Trim();

            if (string.IsNullOrEmpty(schoolName) || string.IsNullOrEmpty(schoolAddress) ||
                string.IsNullOrEmpty(schoolPhone) || string.IsNullOrEmpty(adminEmail))
            {
                InfoBar.IsOpen = true;
                InfoBar.Severity = InfoBarSeverity.Warning;
                InfoBar.Title = "Missing Information";
                InfoBar.Message = "Please fill in all fields.";
                return;
            }

            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            InfoBar.IsOpen = true;
            InfoBar.Severity = InfoBarSeverity.Informational;
            InfoBar.Title = "Setting Up";
            InfoBar.Message = "Generating key pair and saving school information...";

            try
            {
                // Generate key pair in background
                var key = KeyManager.Instance;
                key.GenerateNewKey();

                // Save school info to school settings (same place)
                await SaveSchoolInfoAsync(schoolName, schoolAddress, schoolPhone, adminEmail, key.InstanceAddress);

                InfoBar.Severity = InfoBarSeverity.Success;
                InfoBar.Title = "Success";
                InfoBar.Message = "School information saved. Proceeding to admin account creation...";

                _completed = true;

                // Navigate to admin registration page after short delay
                await Task.Delay(1500);
                Frame.Navigate(typeof(AdminRegistrationView));
            }
            catch (Exception ex)
            {
                InfoBar.Severity = InfoBarSeverity.Error;
                InfoBar.Title = "Error";
                InfoBar.Message = $"Failed to save school information: {ex.Message}";
            }

            ProgressBar.Visibility = Visibility.Collapsed;
        }

        private async Task SaveSchoolInfoAsync(string name, string address, string phone, string email, string instanceAddress)
        {
            // Save to same place as school settings (SchoolSettingsEntity)
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(AppServices.AuthConnectionString)
                .Options;
            using var db = new AppDbContext(options);
            var settings = await db.SchoolSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new Data.Entities.SchoolSettingsEntity
                {
                    Id = 1,
                    SchoolName = name,
                    SchoolAddress = address,
                    SchoolPhone = phone,
                    AdminEmail = email,
                    InstanceAddress = instanceAddress,
                    CreatedAt = DateTime.UtcNow
                };
                db.SchoolSettings.Add(settings);
            }
            else
            {
                settings.SchoolName = name;
                settings.SchoolAddress = address;
                settings.SchoolPhone = phone;
                settings.AdminEmail = email;
                settings.InstanceAddress = instanceAddress;
                db.SchoolSettings.Update(settings);
            }

            await db.SaveChangesAsync();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Go back to login
            Frame.GoBack();
        }
    }
}
