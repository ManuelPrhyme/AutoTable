using AutoTable.Models;
using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class SignUpView : Page
    {
        public SignUpViewModel ViewModel { get; } = new();

        public SignUpView()
        {
            InitializeComponent();
            DataContext = ViewModel;

            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SignUpViewModel.ErrorMessage))
                {
                    ErrorText.Text = ViewModel.ErrorMessage;
                    ErrorText.Visibility = string.IsNullOrWhiteSpace(ViewModel.ErrorMessage)
                        ? Visibility.Collapsed : Visibility.Visible;
                }
            };

            CreateButton.Click += (_, _) =>
            {
                ViewModel.FullName = FullNameBox.Text;
                ViewModel.Email = EmailBox.Text;
                ViewModel.Password = PasswordBox.Password;
                ViewModel.SelectedRole = RoleBox.SelectedIndex == 0
                    ? UserRole.Administrator : UserRole.DataEntrant;
                ViewModel.CreateAccountCommand.Execute(null);
            };
            CancelButton.Click += (_, _) => ViewModel.GoToLoginCommand.Execute(null);
        }
    }
}
