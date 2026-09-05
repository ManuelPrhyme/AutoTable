using AutoTable.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace AutoTable.Views
{
    public sealed partial class AdminRegistrationView : Page
    {
        public AdminRegistrationView()
        {
            InitializeComponent();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var fullName = FullNameBox.Text?.Trim() ?? string.Empty;
            var username = UsernameBox.Text?.Trim() ?? string.Empty;
            var password = PasswordBox.Password ?? string.Empty;
            var confirmPassword = ConfirmPasswordBox.Password ?? string.Empty;

            ErrorText.Visibility = Visibility.Collapsed;

            // Validate
            if (string.IsNullOrWhiteSpace(fullName))
            {
                ShowError("Please enter your full name.");
                return;
            }
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Please enter a username.");
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter a password.");
                return;
            }
            if (password.Length < 4)
            {
                ShowError("Password must be at least 4 characters.");
                return;
            }
            if (password != confirmPassword)
            {
                ShowError("Passwords do not match.");
                return;
            }

            RegisterButton.IsEnabled = false;
            try
            {
                var auth = AuthService.Instance;
                var (success, resetCode) = await auth.RegisterAdministratorAsync(fullName, username, password);

                if (success)
                {
                    // Show the reset code before navigating
                    ShowResetCodeAndContinue(resetCode);
                }
                else
                {
                    ShowError("Registration failed. The username may already be taken, or an administrator already exists.");
                }
            }
            catch (System.Exception ex)
            {
                // Surface the inner DB exception when available — the top-level
                // message for EF SaveChanges failures is generic.
                var detail = ex.InnerException?.Message;
                ShowError(string.IsNullOrWhiteSpace(detail)
                    ? "Registration failed: " + ex.Message
                    : "Registration failed: " + detail);
            }
            finally
            {
                RegisterButton.IsEnabled = true;
            }
        }

        private async void ShowResetCodeAndContinue(string? resetCode)
        {
            var panel = new StackPanel { Spacing = 12 };

            var titleText = new TextBlock
            {
                Text = "Your Credential Reset Code",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                TextWrapping = TextWrapping.WrapWholeWords
            };

            var infoText = new TextBlock
            {
                Text = "Save this code in a secure place. If you ever forget your username or password, you can use this code to reset your credentials.",
                FontSize = 12,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 100, 100)),
                TextWrapping = TextWrapping.WrapWholeWords
            };

            var codeText = new TextBlock
            {
                Text = resetCode ?? "N/A",
                FontSize = 28,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 33, 150, 243)),
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 0, 12)
            };

            panel.Children.Add(titleText);
            panel.Children.Add(infoText);
            panel.Children.Add(codeText);

            var dialog = new ContentDialog
            {
                Title = "Account Created Successfully",
                Content = panel,
                PrimaryButtonText = "Continue to Dashboard",
                CloseButtonText = "Copy Code & Continue",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
            {
                // Copy to clipboard
                var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
                package.SetText(resetCode ?? "");
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            }

            NavigationService.Instance.Navigate(typeof(Views.ShellView));
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
