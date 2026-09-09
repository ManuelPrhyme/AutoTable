using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class SignUpViewModel : BaseViewModel
    {
        [ObservableProperty] private string _fullName = string.Empty;
        [ObservableProperty] private string _username = string.Empty;
        [ObservableProperty] private string _password = string.Empty;
        [ObservableProperty] private string _confirmPassword = string.Empty;
        [ObservableProperty] private UserRole _selectedRole = UserRole.Administrator;
        [ObservableProperty] private string _errorMessage = string.Empty;
        [ObservableProperty] private bool _isBusy;

        [RelayCommand]
        private async Task CreateAccountAsync()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            // Validate all fields
            if (string.IsNullOrWhiteSpace(FullName) ||
                string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password))
            {
                IsBusy = false;
                ErrorMessage = "Please fill in all fields to create an account.";
                return;
            }

            // Validate password confirmation
            if (Password != ConfirmPassword)
            {
                IsBusy = false;
                ErrorMessage = "Passwords do not match. Please re-enter your password.";
                return;
            }

            var ok = await AuthService.Instance.SignUpAsync(FullName, Username, Password, SelectedRole);
            IsBusy = false;

            if (ok)
                NavigationService.Instance.Navigate(typeof(Views.ShellView));
            else
                ErrorMessage = "Please fill in all fields to create an account.";
        }

        [RelayCommand]
        private void GoToLogin() =>
            NavigationService.Instance.Navigate(typeof(Views.LoginView));
    }
}
