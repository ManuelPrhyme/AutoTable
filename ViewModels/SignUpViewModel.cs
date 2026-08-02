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
        [ObservableProperty] private string _email = string.Empty;
        [ObservableProperty] private string _password = string.Empty;
        [ObservableProperty] private UserRole _selectedRole = UserRole.DataEntrant;
        [ObservableProperty] private string _errorMessage = string.Empty;
        [ObservableProperty] private bool _isBusy;

        [RelayCommand]
        private async Task CreateAccountAsync()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            var ok = await AuthService.Instance.SignUpAsync(FullName, Email, Password, SelectedRole);
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
