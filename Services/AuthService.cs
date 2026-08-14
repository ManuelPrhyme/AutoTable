using AutoTable.Models;
using System.Threading.Tasks;

namespace AutoTable.Services
{
    public class AuthService : IAuthService
    {
        private static AuthService? _instance;
        public static AuthService Instance => _instance ??= new AuthService();

        public Task<bool> SignInAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Task.FromResult(false);

            var user = new User
            {
                Email = username.Trim(),
                FullName = username,
                Role = username.StartsWith("admin", System.StringComparison.OrdinalIgnoreCase)
                    ? UserRole.Administrator
                    : UserRole.DataEntrant
            };

            SessionService.Instance.SetUser(user);
            return Task.FromResult(true);
        }

        public Task<bool> SignUpAsync(string fullName, string username, string password, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
                return Task.FromResult(false);

            SessionService.Instance.SetUser(new User
            {
                FullName = fullName.Trim(),
                Email = username.Trim(),
                Role = role
            });

            return Task.FromResult(true);
        }
    }
}
