using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        [ObservableProperty] private string _username = string.Empty;
        [ObservableProperty] private string _password = string.Empty;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _errorMessage = string.Empty;

        [RelayCommand]
        public async Task SignIn()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            var ok = await AuthService.Instance.SignInAsync(Username, Password);
            IsBusy = false;

            if (ok)
                NavigationService.Instance.Navigate(typeof(Views.ShellView));
            else
                ErrorMessage = "Enter a valid username and password.";
        }

        [RelayCommand]
        private void GoToSignUp() =>
            NavigationService.Instance.Navigate(typeof(Views.DataEntrantRegistrationView));
    }

}
