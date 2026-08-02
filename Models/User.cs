namespace AutoTable.Models
{
    public class User
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.DataEntrant;
    }
}
