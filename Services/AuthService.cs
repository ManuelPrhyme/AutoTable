using AutoTable.Models;
using System.Threading.Tasks;

namespace AutoTable.Services
{
    public class AuthService : IAuthService
    {
        private static AuthService? _instance;
        public static AuthService Instance => _instance ??= new AuthService();

        public Task<bool> SignInAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Task.FromResult(false);

            var user = new User
            {
                Email = email.Trim(),
                FullName = email.Contains('@') ? email.Split('@')[0] : email,
                Role = email.StartsWith("admin", System.StringComparison.OrdinalIgnoreCase)
                    ? UserRole.Administrator
                    : UserRole.DataEntrant
            };

            SessionService.Instance.SetUser(user);
            return Task.FromResult(true);
        }

        public Task<bool> SignUpAsync(string fullName, string email, string password, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
                return Task.FromResult(false);

            SessionService.Instance.SetUser(new User
            {
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Role = role
            });

            return Task.FromResult(true);
        }
    }
}
