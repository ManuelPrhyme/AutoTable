using AutoTable.Models;
using System.Threading.Tasks;

namespace AutoTable.Services
{
    public interface IAuthService
    {
        /// <summary>True when at least one Administrator account exists in the DB.</summary>
        Task<bool> HasAdministratorAsync();

        /// <summary>Register the first administrator (fails if one already exists).</summary>
        Task<(bool Success, string? ResetCode)> RegisterAdministratorAsync(string fullName, string username, string password);

        /// <summary>Sign in with username + password against the DB.</summary>
        Task<bool> SignInAsync(string email, string password);

        /// <summary>Verify the current user's password against the DB.
        /// In demo mode (no auth connection string), any non-empty password is accepted.
        /// Returns true if the password is correct, false otherwise.</summary>
        Task<bool> VerifyPasswordAsync(string password);

        /// <summary>Sign up a data entrant using an admin-generated invite code.</summary>
        Task<(bool Success, string? Error)> SignUpWithInviteCodeAsync(
            string fullName, string username, string password, string inviteCode);

        /// <summary>Legacy sign-up (kept for backward compatibility).</summary>
        Task<bool> SignUpAsync(string fullName, string email, string password, UserRole role);
    }
}
