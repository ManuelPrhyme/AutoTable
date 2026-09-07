using AutoTable.Services;
using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class LoginView : Page
    {
        public LoginViewModel ViewModel { get; } = new();

        public LoginView()
        {
            InitializeComponent();
            DataContext = ViewModel;

            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(LoginViewModel.ErrorMessage))
                {
                    ErrorText.Text = ViewModel.ErrorMessage;
                    ErrorText.Visibility = string.IsNullOrWhiteSpace(ViewModel.ErrorMessage)
                        ? Visibility.Collapsed : Visibility.Visible;
                }
            };

            SignInButton.Click += async (_, _) =>
            {
                ViewModel.Username = UsernameBox.Text;
                ViewModel.Password = PasswordBox.Password;
                try
                {
                    // Await the async command so we can observe failures and ensure navigation occurs
                    await ViewModel.SignInCommand.ExecuteAsync(null);
                }
                catch (System.Exception ex)
                {
                    ErrorText.Text = "Sign in failed: " + ex.Message;
                    ErrorText.Visibility = Visibility.Visible;
                }
                //NavigationService.Instance.Navigate(typeof(Views.AssessmentsView));
            };
            SignUpLink.Click += (_, _) => NavigationService.Instance.Navigate(typeof(Views.DataEntrantRegistrationView));
            ForgotCredentialsLink.Click += ForgotCredentialsLink_Click;

            SignUpContainer.PointerEntered += SignUpContainer_PointerEntered;
            SignUpContainer.PointerExited += SignUpContainer_PointerExited;
        }

        private void SignUpContainer_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            SignUpContainer.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightBlue);
        }

        private void SignUpContainer_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            SignUpContainer.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        private async void ForgotCredentialsLink_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var panel = new StackPanel { Spacing = 12 };

            var infoText = new TextBlock
            {
                Text = "Enter your credential reset code provided by your administrator, then set a new username and password.",
                FontSize = 12,
                TextWrapping = TextWrapping.WrapWholeWords
            };

            var resetCodeBox = new TextBox { PlaceholderText = "e.g. AB12-CD34" };
            var newUsernameBox = new TextBox { Header = "New Username", PlaceholderText = "Enter new username" };
            var newPasswordBox = new PasswordBox { Header = "New Password", PlaceholderText = "Enter new password" };
            var confirmPasswordBox = new PasswordBox { Header = "Confirm Password", PlaceholderText = "Re-enter new password" };

            var errorText = new TextBlock
            {
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 220, 50, 50)),
                TextWrapping = TextWrapping.WrapWholeWords,
                Visibility = Microsoft.UI.Xaml.Visibility.Collapsed
            };

            panel.Children.Add(infoText);
            panel.Children.Add(resetCodeBox);
            panel.Children.Add(newUsernameBox);
            panel.Children.Add(newPasswordBox);
            panel.Children.Add(confirmPasswordBox);
            panel.Children.Add(errorText);

            var dialog = new ContentDialog
            {
                Title = "Reset Credentials",
                Content = panel,
                PrimaryButtonText = "Reset",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            // Validate
            if (resetCodeBox.Text.Length == 0 || newUsernameBox.Text.Length == 0 ||
                newPasswordBox.Password.Length == 0 || confirmPasswordBox.Password.Length == 0)
            {
                errorText.Text = "All fields are required.";
                errorText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                return;
            }

            if (newPasswordBox.Password != confirmPasswordBox.Password)
            {
                errorText.Text = "Passwords do not match.";
                errorText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                return;
            }

            try
            {
                var (success, error) = await AuthService.Instance.ResetCredentialsAsync(
                    resetCodeBox.Text, newUsernameBox.Text, newPasswordBox.Password);

                if (success)
                {
                    var successDialog = new ContentDialog
                    {
                        Title = "Success",
                        Content = "Your credentials have been reset. You can now sign in with your new username and password.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await successDialog.ShowAsync();
                }
                else
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Invalid Reset Code",
                        Content = error ?? "The reset code you entered is invalid or has already been used. Please check with your administrator for a valid code.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
            catch (System.Exception ex)
            {
                errorText.Text = "Reset failed: " + ex.Message;
                errorText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            }
        }
    }
}
