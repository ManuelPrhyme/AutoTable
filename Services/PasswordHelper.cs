using System;
using System.Security.Cryptography;
using System.Text;

namespace AutoTable.Services
{
    /// <summary>
    /// Simple password hashing using SHA-256 with a per-user random salt.
    /// Suitable for a desktop app with no remote-attack surface.
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Hash a plaintext password with a new random salt.
        /// Returns a combined "salt:hash" string for storage.
        /// </summary>
        public static string HashPassword(string password)
        {
            var saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(saltBytes);

            var salt = Convert.ToBase64String(saltBytes);
            var hash = ComputeHash(password, saltBytes);

            return $"{salt}:{hash}";
        }

        /// <summary>
        /// Verify a plaintext password against a stored "salt:hash" string.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            var parts = storedHash.Split(':');
            if (parts.Length != 2) return false;

            var salt = parts[0];
            var expectedHash = parts[1];

            var saltBytes = Convert.FromBase64String(salt);
            var actualHash = ComputeHash(password, saltBytes);

            return string.Equals(actualHash, expectedHash, StringComparison.Ordinal);
        }

        private static string ComputeHash(string password, byte[] saltBytes)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            // Concatenate salt + password
            var combined = new byte[saltBytes.Length + passwordBytes.Length];
            Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, combined, saltBytes.Length, passwordBytes.Length);

            var hashBytes = SHA256.HashData(combined);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
