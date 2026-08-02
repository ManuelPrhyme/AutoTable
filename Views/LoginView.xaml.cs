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

            SignInButton.Click += (_, _) =>
            {
                ViewModel.Email = EmailBox.Text;
                ViewModel.Password = PasswordBox.Password;
                ViewModel.SignInCommand.Execute(null);
                //NavigationService.Instance.Navigate(typeof(Views.AssessmentsView));
            };
            SignUpLink.Click += (_, _) => ViewModel.GoToSignUpCommand.Execute(null);
        }
    }
}
