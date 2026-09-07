using System;

namespace AutoTable.Data.Entities
{
    /// <summary>
    /// Records every operation performed by any user in the application.
    /// Used for auditing, tracking, and analytics.
    /// </summary>
    public class AuditLogEntry
    {
        public int Id { get; set; }
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string? Details { get; set; }
        public int IsSuccess { get; set; } = 1;
        public string? ErrorMessage { get; set; }
        public string? IpAddress { get; set; }
        public string? SessionId { get; set; }
    }
}
