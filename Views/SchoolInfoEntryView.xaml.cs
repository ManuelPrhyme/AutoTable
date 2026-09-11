using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoTable.Data;
using AutoTable.Services;

namespace AutoTable.Views
{
    public sealed partial class SchoolInfoEntryView : Page
    {
        private bool _completed = false;
        private string? _pendingSchoolName;
        private string? _pendingAdminEmail;

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
                // Reuse the key pair created at app startup (KeyManager.Initialize());
                // regenerate a fresh one as a last resort if the loaded key is incomplete.
                var key = KeyManager.Instance;
                if (!key.HasKey || string.IsNullOrEmpty(key.InstanceAddress))
                {
                    key.GenerateNewKey();
                }

                if (string.IsNullOrEmpty(key.InstanceAddress))
                {
                    throw new InvalidOperationException(
                        "Could not derive this device's blockchain instance address. " +
                        "Please restart the app and try again.");
                }

                var instanceAddress = key.InstanceAddress;

                // Save school info to school settings (same place)
                await SaveSchoolInfoAsync(schoolName, schoolAddress, schoolPhone, adminEmail, instanceAddress);

                // Online steps: request gas from the faucet, AWAIT the server's
                // confirmation that the funds were sent (the server only responds
                // 2xx AFTER its drip transaction is mined with status 'success'),
                // then verify the instance balance is actually > 0 (1 check + 7
                // retries at 5s intervals) BEFORE sending the registration tx.
                // If funding can't be confirmed, a manual override button lets the
                // user re-request tokens instead of registering without gas.
                _pendingSchoolName = schoolName;
                _pendingAdminEmail = adminEmail;

                // --- ONLINE PHASE ---
                // Only attempt online steps when a faucet server is configured.
                if (!string.IsNullOrEmpty(AppServices.FaucetApiUrl))
                {
                    await RequestGasAndRegisterAsync(schoolName, adminEmail);
                }
                else
                {
                    // No faucet configured. Save school info + key locally but
                    // do NOT attempt on-chain registration (no gas source).
                    InfoBar.Severity = InfoBarSeverity.Warning;
                    InfoBar.Title = "No Faucet Configured";
                    InfoBar.Message = "School information saved locally. No faucet server is configured, so registration on-chain is deferred. Contact your administrator to configure the faucet or request gas manually.";
                    ProgressBar.Visibility = Visibility.Collapsed;
                }

                await ProceedToActivationAsync();
            }
            catch (Exception ex)
            {
                // A bare NullReferenceException message is useless for diagnosis —
                // log the full exception (incl. stack trace) to the Desktop log and
                // surface the failing frame in the InfoBar.
                var details = ex.InnerException != null
                    ? $"{ex.Message} -> {ex.InnerException.Message}"
                    : ex.Message;
                var failingFrame = ex.StackTrace?
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(f => f.TrimStart().StartsWith("at AutoTable", StringComparison.Ordinal))?
                    .Trim() ?? "(no AutoTable frame)";

                try
                {
                    var logPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AutoTable_logs.txt");
                    System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SchoolInfoEntryView.ContinueButton_Click] {Environment.NewLine}{ex}{Environment.NewLine}{new string('-', 60)}{Environment.NewLine}");
                }
                catch { /* logging must never crash the app */ }

                InfoBar.Severity = InfoBarSeverity.Error;
                InfoBar.Title = "Error";
                InfoBar.Message = $"Failed to save school information: {details}{Environment.NewLine}{failingFrame}";
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

        /// <summary>
        /// Manual override shown when the balance gate could not be cleared:
        /// re-requests gas tokens from the faucet (awaiting the server's on-chain
        /// confirmation), waits for the balance with 1 + 7 checks at 5s intervals,
        /// and only then sends the registration transaction and proceeds.
        /// </summary>
        // ---------- Helpers ----------

        /// <summary>
        /// Orchestrates the gas-request → balance-wait → register sequence so the
        /// registration transaction never fires without confirmed gas.
        ///
        /// The app .NET client receives the faucet server's HTTP 2xx only after the
        /// server's drip transaction is mined with status 'success' (see
        /// FaucetServerside/src/services/faucetService.js -> requestDrip). That
        /// still does not guarantee the drip landed in the wallet in time, so we
        /// then poll the balance (1 check + 7 retries @5s) before registering.
        /// </summary>
        private async Task RequestGasAndRegisterAsync(string schoolName, string adminEmail)
        {
            var faucetUrl = AppServices.FaucetApiUrl;
            if (string.IsNullOrEmpty(faucetUrl)) return;

            // The loader stays VISIBLE and INDETERMINATE for the WHOLE automatic
            // sequence (faucet request → on-chain confirmation → balance polling
            // → registration). It is only hidden when the automatic process fails
            // (at which point the manual "Request Tokens Again" button appears) or
            // when the caller's finally/epilogue collapses it on success.
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ContinueButton.IsEnabled = false;

            try
            {
                InfoBar.Severity = InfoBarSeverity.Informational;
                InfoBar.Title = "Requesting Gas";
                InfoBar.Message = "Requesting gas tokens from the faucet... (this can take up to a minute — waiting for on-chain confirmation)";

                // NOTE: no intermediate IsIndeterminate toggles here — the loader
                // must remain on display until the whole period is done.
                var faucetResult = await AppServices.License.RequestGasTokensAsync(faucetUrl);

                if (!faucetResult.Ok)
                {
                    // Automatic process failed → hide loader, show manual override.
                    ProgressBar.IsIndeterminate = false;
                    ProgressBar.Visibility = Visibility.Collapsed;
                    RequestTokensAgainButton.Visibility = Visibility.Visible;
                    InfoBar.Severity = InfoBarSeverity.Error;
                    InfoBar.Title = "Gas Request Failed";
                    InfoBar.Message = $"{faucetResult.Detail} You can retry below or cancel to continue offline.";
                    return;
                }

                // Server confirmed the funds were sent (tx mined, status success).
                // Now wait for the balance to actually reflect in the instance wallet
                // before sending the registration transaction (1 + 7 retries @ 5s).
                InfoBar.Title = "Verifying Balance";
                InfoBar.Message = "Funds confirmed on-chain. Checking instance balance (up to 7 checks, 5s apart)...";

                var funded = await AppServices.License.WaitForFundingAsync(retryCount: 7, intervalSeconds: 5);

                if (!funded)
                {
                    // Automatic process failed → hide loader, show manual override.
                    ProgressBar.IsIndeterminate = false;
                    ProgressBar.Visibility = Visibility.Collapsed;
                    RequestTokensAgainButton.Visibility = Visibility.Visible;
                    InfoBar.Severity = InfoBarSeverity.Warning;
                    InfoBar.Title = "Funds Not Detected";
                    InfoBar.Message = "Funds were sent from the faucet but the balance is still zero after 7 retries. Click \"Request Tokens Again\" to retry, or cancel to continue offline.";
                    return;
                }

                InfoBar.Title = "Registering";
                InfoBar.Message = "Balance confirmed. Registering school on-chain...";
                await AppServices.License.RegisterSchoolAsync(schoolName, adminEmail);
            }
            finally
            {
                ContinueButton.IsEnabled = true;
                // On success the caller's epilogue collapses the ProgressBar; on the
                // failure returns above it was already collapsed. Collapse here too
                // so the loader never lingers after the orchestration ends.
                ProgressBar.IsIndeterminate = false;
            }
        }

        private async void RequestTokensAgain_Click(object sender, RoutedEventArgs e)
        {
            var schoolName = _pendingSchoolName;
            var adminEmail = _pendingAdminEmail;
            if (string.IsNullOrEmpty(schoolName) || string.IsNullOrEmpty(adminEmail)) return;

            RequestTokensAgainButton.Visibility = Visibility.Collapsed;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            InfoBar.IsOpen = true;

            try
            {
                await RequestGasAndRegisterAsync(schoolName, adminEmail);
                RequestTokensAgainButton.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                RequestTokensAgainButton.Visibility = Visibility.Visible;
                InfoBar.Severity = InfoBarSeverity.Error;
                InfoBar.Title = "Error";
                InfoBar.Message = ex.Message;
                ProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            await ProceedToActivationAsync();
        }

        /// <summary>
        /// Shows the license activation dialog, then proceeds to admin account
        /// creation. Shared by the Continue flow and the manual-override retry.
        ///
        /// The dialog is MANDATORY and blocking: the user must enter a valid
        /// activation code (verified on-chain) before they can reach admin
        /// account creation. There is no cancel path, and the loop re-shows the
        /// dialog if it is closed by any other means.
        /// </summary>
        private async Task ProceedToActivationAsync()
        {
            InfoBar.Severity = InfoBarSeverity.Success;
            InfoBar.Title = "Success";
            InfoBar.Message = "School information saved. Please enter your activation code to proceed.";

            _completed = true;

            // The license-activation dialog is MANDATORY and blocking: there is no
            // cancel button, close-button clicks are cancelled, light-dismiss is
            // disabled, and the loop re-shows the dialog if it is closed by any
            // other path. It only returns after a successful activation — the user
            // cannot bypass this step to reach admin account creation.
            while (true)
            {
                var activationDialog = new LicenseActivationDialog
                {
                    XamlRoot = this.XamlRoot
                };
                await activationDialog.ShowAsync();
                if (activationDialog.Activated)
                    break;
            }

            InfoBar.Severity = InfoBarSeverity.Success;
            InfoBar.Title = "Activated";
            InfoBar.Message = "License activated. Proceeding to admin account creation...";

            await Task.Delay(1000);
            Frame.Navigate(typeof(AdminRegistrationView));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Go back to login. On first launch this is the first page in the
            // frame — GoBack() throws if there is no back stack entry.
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}

