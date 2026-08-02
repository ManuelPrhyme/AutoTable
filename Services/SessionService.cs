using AutoTable.Models;

namespace AutoTable.Services
{
    public class SessionService
    {
        private static SessionService? _instance;
        public static SessionService Instance => _instance ??= new SessionService();

        public User? CurrentUser { get; private set; }

        public bool IsAuthenticated => CurrentUser != null;

        public void SetUser(User user) => CurrentUser = user;

        public void SignOut() => CurrentUser = null;

        public bool IsAdministrator => CurrentUser?.Role == UserRole.Administrator;
    }
}
