namespace AutoTable.Models
{
    public class User
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.DataEntrant;

        /// <summary>DB row id of the authenticated user (null for legacy/mock sessions).</summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Comma-separated page tags this user may access, inherited from the invite code
        /// used at registration. Null or empty means full access.
        /// </summary>
        public string? AllowedPages { get; set; }
    }
}
