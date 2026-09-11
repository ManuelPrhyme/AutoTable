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
                var faucetUrl = AppServices.FaucetApiUrl;
                if (string.IsNullOrEmpty(faucetUrl))
                {
                    InfoBar.Severity = InfoBarSeverity.Error;
                    InfoBar.Title = "No Faucet";
                    InfoBar.Message = "Faucet server URL is not configured.";
                    ProgressBar.Visibility = Visibility.Collapsed;
                    return;
                }

                // RequestGasTokensAsync AWAITS the faucet server's confirmation —
                // the server only responds 2xx AFTER its drip transaction is mined
                // with status 'success', so Ok=true means the funds were SENT.
                var faucetResult = await AppServices.License.RequestGasTokensAsync(faucetUrl);

                if (faucetResult.Ok)
                {
                    InfoBar.Severity = InfoBarSeverity.Success;
                    InfoBar.Title = "Success";
                    InfoBar.Message = "Gas tokens sent (confirmed on-chain). Balance will update shortly.";
                }
                else
                {
                    InfoBar.Severity = InfoBarSeverity.Error;
                    InfoBar.Title = "Failed";
                    InfoBar.Message = faucetResult.Detail ?? "Failed to request gas tokens. Please try again later.";
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
                InfoBar.Title = "Verifying";
                InfoBar.Message = "Verifying code against the blockchain...";

                var instanceAddress = AppServices.Key.InstanceAddress ?? "";
                if (string.IsNullOrEmpty(instanceAddress))
                    throw new InvalidOperationException("No instance address available. Restart the app and try again.");

                // 1. Static verification (no gas): is this code active + assigned to this school?
                var (isValid, periodSeconds, graceSeconds, _) = await AppServices.License.VerifyCodeAsync(code, instanceAddress);
                if (!isValid)
                {
                    InfoBar.Severity = InfoBarSeverity.Error;
                    InfoBar.Title = "Invalid Code";
                    InfoBar.Message = "This code is not active, is expired, or was not issued for this school instance.";
                    return;
                }

                // 2. Activate on-chain (signed transaction) — waits for tx to be mined
                //    and parses the LicenseActivated event to get on-chain parameters.
                InfoBar.Title = "Activating";
                InfoBar.Message = "Submitting activation transaction...";
                var result = await AppServices.License.ActivateLicenseAsync(code);

                // 3. Create the local license file with the on-chain period + grace
                //    (parsed from the LicenseActivated event, not from verifyCode —
                //    these are the authoritative vendor-assigned values).
                AppServices.LicenseManager.Activate(
                    periodMinutes: Math.Max(1, result.PeriodSeconds / 60L),
                    graceMinutes: Math.Max(1, result.GraceSeconds / 60L),
                    activationCode: code,
                    schoolCode: instanceAddress,
                    instanceAddress: instanceAddress);

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
            // This dialog is mandatory — the user must enter an activation code to proceed.
            // Cancel any close-button (X / system-dismiss) attempt so the dialog stays open
            // until activation succeeds.
            args.Cancel = true;
        }
    }
}
