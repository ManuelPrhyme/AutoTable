using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using AutoTable.Services;

namespace AutoTable.Views
{
    public sealed partial class LicenseActivationDialog : ContentDialog
    {
        private bool _activated = false;

        public bool Activated => _activated;

        public LicenseActivationDialog()
        {
            this.InitializeComponent();

            // Display instance info
            var key = AppServices.Key;
            if (key.HasKey)
            {
                InstanceIdRun.Text = key.InstanceAddress;
            }
            else
            {
                InstanceIdRun.Text = "No key generated";
            }

            // Load balance
            LoadBalanceAsync();
        }

        private async void LoadBalanceAsync()
        {
            try
            {
                var balance = await AppServices.License.GetBalanceAsync();
                BalanceRun.Text = $"{balance:F6} ETH";
            }
            catch
            {
                BalanceRun.Text = "Unable to fetch";
            }
        }

        private async void RequestGas_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            InfoBar.IsOpen = true;
            InfoBar.Severity = InfoBarSeverity.Informational;
            InfoBar.Title = "Requesting";
            InfoBar.Message = "Requesting gas tokens from faucet...";

            try
            {
                // Replace with your faucet API URL
                var success = await AppServices.License.RequestGasTokensAsync("https://your-faucet-api.com");

                if (success)
                {
                    InfoBar.Severity = InfoBarSeverity.Success;
                    InfoBar.Title = "Success";
                    InfoBar.Message = "Gas tokens requested. Please wait for confirmation.";
                }
                else
                {
                    InfoBar.Severity = InfoBarSeverity.Error;
                    InfoBar.Title = "Failed";
                    InfoBar.Message = "Failed to request gas tokens. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                InfoBar.Severity = InfoBarSeverity.Error;
                InfoBar.Title = "Error";
                InfoBar.Message = $"Error: {ex.Message}";
            }

            ProgressBar.Visibility = Visibility.Collapsed;
            LoadBalanceAsync();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();

            try
            {
                var code = ActivationCodeBox.Text?.Trim();
                if (string.IsNullOrEmpty(code))
                {
                    InfoBar.IsOpen = true;
                    InfoBar.Severity = InfoBarSeverity.Warning;
                    InfoBar.Title = "Invalid";
                    InfoBar.Message = "Please enter an activation code.";
                    deferral.Complete();
                    return;
                }

                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.IsIndeterminate = true;
                InfoBar.IsOpen = true;
                InfoBar.Severity = InfoBarSeverity.Informational;
                InfoBar.Title = "Activating";
                InfoBar.Message = "Activating license on blockchain...";

                // Activate on blockchain
                var txHash = await AppServices.License.ActivateLicenseAsync(code);

                // Get expiry from blockchain
                var expiry = await AppServices.License.GetExpiryAsync();

                // Update local license tracker
                AppServices.LicenseTracker.Activate(code, expiry ?? DateTime.UtcNow.AddYears(1), AppServices.Key.InstanceAddress ?? "");

                InfoBar.Severity = InfoBarSeverity.Success;
                InfoBar.Title = "Activated";
                InfoBar.Message = "License activated successfully!";

                AppServices.Toasts.Show("License Activated", "Your license has been activated successfully.");

                _activated = true;

                // Close dialog after short delay
                await Task.Delay(1500);
                Hide();
            }
            catch (Exception ex)
            {
                InfoBar.Severity = InfoBarSeverity.Error;
                InfoBar.Title = "Activation Failed";
                InfoBar.Message = ex.Message;
            }

            ProgressBar.Visibility = Visibility.Collapsed;
            deferral.Complete();
        }

        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // User cancelled
        }
    }
}
