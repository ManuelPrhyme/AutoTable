using AutoTable.Data;
using AutoTable.Data.Entities;
using AutoTable.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace AutoTable.Services
{
    public class AuthService : IAuthService
    {
        private static AuthService? _instance;
        public static AuthService Instance => _instance ??= new AuthService();

        private DbContextOptions<AppDbContext>? _options;

        private DbContextOptions<AppDbContext> GetOptions()
        {
            if (_options == null)
            {
                var connStr = AppServices.AuthConnectionString
                    ?? throw new InvalidOperationException("AuthConnectionString not set in AppServices.");
                _options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connStr)
                    .AddInterceptors(new AppDbContext.ForeignKeyInterceptor())
                    .Options;
            }
            return _options;
        }

        // ── Check whether an administrator account exists ─────────

        public async Task<bool> HasAdministratorAsync()
        {
            using var db = new AppDbContext(GetOptions());
            return await db.Users.AnyAsync(u => u.Role == "Administrator");
        }

        // ── Admin registration (first-time setup) ────────────────

        public async Task<(bool Success, string? ResetCode)> RegisterAdministratorAsync(string fullName, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
                return (false, null);

            using var db = new AppDbContext(GetOptions());

            // Prevent duplicate registration if an admin already exists
            if (await db.Users.AnyAsync(u => u.Role == "Administrator"))
                return (false, null);

            // Prevent duplicate username
            if (await db.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == username.Trim().ToLower()))
                return (false, null);

            var user = new UserEntity
            {
                FullName = fullName.Trim(),
                Email = username.Trim().ToLower(),
                PasswordHash = PasswordHelper.HashPassword(password),
                Role = "Administrator",
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Generate a credential reset code for the admin
            var resetCode = GenerateRandomCode();
            user.CredentialResetCode = resetCode;
            await db.SaveChangesAsync();

            // Set session
            SessionService.Instance.SetUser(new User
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = UserRole.Administrator,
                UserId = user.Id
            });

            return (true, resetCode);
        }

        // ── Sign-in with real DB lookup ──────────────────────────

        public async Task<bool> SignInAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            // Demo mode (AUTOTABLE_DEMO_MODE=true): no DB connection string is
            // configured, so there are no stored users. Accept any non-empty
            // credentials as an Administrator so the demo app stays usable.
            if (string.IsNullOrEmpty(AppServices.AuthConnectionString))
            {
                SessionService.Instance.SetUser(new User
                {
                    FullName = username.Trim(),
                    Email = username.Trim().ToLower(),
                    Role = UserRole.Administrator
                });
                return true;
            }

            using var db = new AppDbContext(GetOptions());

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == username.Trim().ToLower());

            if (user == null) return false;

            // Verify password
            if (string.IsNullOrEmpty(user.PasswordHash))
                return false;

            if (!PasswordHelper.VerifyPassword(password, user.PasswordHash))
                return false;

            // Map role
            var role = string.Equals(user.Role, "Administrator", StringComparison.OrdinalIgnoreCase)
                ? UserRole.Administrator
                : UserRole.DataEntrant;

            SessionService.Instance.SetUser(new User
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = role,
                UserId = user.Id,
                AllowedPages = user.AllowedPages
            });

            return true;
        }

        // ── Invite-code sign-up (data entrant) ───────────────────

        public async Task<(bool Success, string? Error)> SignUpWithInviteCodeAsync(
            string fullName, string username, string password, string inviteCode)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(inviteCode))
                return (false, "All fields are required.");

            using var db = new AppDbContext(GetOptions());

            // Find the invite code
            var invite = await db.InviteCodes
                .FirstOrDefaultAsync(i => i.Code == inviteCode.Trim().ToUpper());

            if (invite == null)
                return (false, "Invalid invite code.");

            if (invite.IsUsed)
                return (false, "This invite code has already been used.");

            if (invite.ExpiresAt.HasValue && invite.ExpiresAt.Value < DateTime.UtcNow)
                return (false, "This invite code has expired.");

            // Prevent duplicate username
            if (await db.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == username.Trim().ToLower()))
                return (false, "A user with this username already exists.");

            var user = new UserEntity
            {
                FullName = fullName.Trim(),
                Email = username.Trim().ToLower(),
                PasswordHash = PasswordHelper.HashPassword(password),
                Role = invite.Role,
                AllowedPages = invite.AllowedPages,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            invite.IsUsed = true;
            db.InviteCodes.Update(invite);
            await db.SaveChangesAsync();

            // Set session
            var role = string.Equals(invite.Role, "Administrator", StringComparison.OrdinalIgnoreCase)
                ? UserRole.Administrator
                : UserRole.DataEntrant;

            SessionService.Instance.SetUser(new User
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = role,
                UserId = user.Id,
                AllowedPages = invite.AllowedPages
            });

            return (true, null);
        }

        // ── Credential reset (forgot username / password) ────────

        /// <summary>
        /// Generate a new credential reset code for a user. Returns the generated code,
        /// or null if the user was not found. The code is stored on the UserEntity.
        /// </summary>
        public async Task<string?> GenerateCredentialResetCodeAsync(int userId)
        {
            using var db = new AppDbContext(GetOptions());
            var user = await db.Users.FindAsync(userId);
            if (user == null) return null;

            var code = GenerateRandomCode();
            user.CredentialResetCode = code;
            await db.SaveChangesAsync();
            return code;
        }

        /// <summary>
        /// Verify a credential reset code and update the user's username and password.
        /// Returns true on success, false if the code is invalid or user not found.
        /// Generates a NEW reset code after successful use (one-time use).
        /// </summary>
        public async Task<(bool Success, string? Error)> ResetCredentialsAsync(
            string resetCode, string newUsername, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(resetCode) ||
                string.IsNullOrWhiteSpace(newUsername) ||
                string.IsNullOrWhiteSpace(newPassword))
                return (false, "All fields are required.");

            if (newPassword.Length < 4)
                return (false, "Password must be at least 4 characters.");

            using var db = new AppDbContext(GetOptions());
            var user = await db.Users.FirstOrDefaultAsync(
                u => u.CredentialResetCode == resetCode.Trim().ToUpper());

            if (user == null)
                return (false, "Invalid reset code. Contact your administrator.");

            // Check for duplicate username (excluding current user)
            if (await db.Users.AnyAsync(u =>
                u.Id != user.Id &&
                u.Email != null &&
                u.Email.ToLower() == newUsername.Trim().ToLower()))
                return (false, "A user with this username already exists.");

            user.Email = newUsername.Trim().ToLower();
            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            user.CredentialResetCode = GenerateRandomCode(); // rotate code after use
            await db.SaveChangesAsync();

            return (true, null);
        }

        private static string GenerateRandomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I/1/O/0
            var random = new Random();
            var part1 = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
            var part2 = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
            return $"{part1}-{part2}";
        }

        // ── Legacy sign-up (kept for compatibility) ──────────────

        public Task<bool> SignUpAsync(string fullName, string email, string password, UserRole role)
        {
            // This is the old non-DB path; kept only so existing call sites don't break.
            // New code should use RegisterAdministratorAsync or SignUpWithInviteCodeAsync.
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
