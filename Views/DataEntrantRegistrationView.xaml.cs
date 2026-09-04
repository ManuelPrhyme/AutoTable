using AutoTable.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class DataEntrantRegistrationView : Page
    {
        public DataEntrantRegistrationView()
        {
            InitializeComponent();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var inviteCode = InviteCodeBox.Text?.Trim() ?? string.Empty;
            var fullName = FullNameBox.Text?.Trim() ?? string.Empty;
            var username = UsernameBox.Text?.Trim() ?? string.Empty;
            var password = PasswordBox.Password ?? string.Empty;
            var confirmPassword = ConfirmPasswordBox.Password ?? string.Empty;

            ErrorText.Visibility = Visibility.Collapsed;

            // Validate
            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                ShowError("Please enter your invite code.");
                return;
            }
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
                var (success, error) = await auth.SignUpWithInviteCodeAsync(fullName, username, password, inviteCode);

                if (success)
                {
                    NavigationService.Instance.Navigate(typeof(Views.ShellView));
                }
                else
                {
                    ShowError(error ?? "Registration failed. Please check your invite code and try again.");
                }
            }
            catch (System.Exception ex)
            {
                ShowError("Registration failed: " + ex.Message);
            }
            finally
            {
                RegisterButton.IsEnabled = true;
            }
        }

        private void BackToLoginButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.Navigate(typeof(Views.LoginView));
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
