using AutoTable.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
                var success = await auth.RegisterAdministratorAsync(fullName, username, password);

                if (success)
                {
                    NavigationService.Instance.Navigate(typeof(Views.ShellView));
                }
                else
                {
                    ShowError("Registration failed. The username may already be taken, or an administrator already exists.");
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

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
