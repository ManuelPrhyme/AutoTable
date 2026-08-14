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
            SignUpLink.Click += (_, _) => ViewModel.GoToSignUpCommand.Execute(null);
        }
    }
}
