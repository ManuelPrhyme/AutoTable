using System;
using AutoTable.Data.Entities;

namespace AutoTable.Models
{
    /// <summary>
    /// UI-friendly wrapper over an <see cref="AuditLogEntry"/>.
    /// Provides plain-string display helpers so the row template stays
    /// framework-agnostic (same pattern as <see cref="TerminationLogItem"/>).
    /// </summary>
    public class AuditLogRow
    {
        public AuditLogEntry Entry { get; }

        public AuditLogRow(AuditLogEntry entry)
        {
            Entry = entry;
        }

        public int Id => Entry.Id;
        public string UserName => Entry.UserName;
        public string? UserRole => Entry.UserRole;
        public string Category => Entry.Category;
        public string Operation => Entry.Operation;
        public string EntityType => Entry.EntityType;
        public string? EntityName => Entry.EntityName;
        public string? Details => Entry.Details;

        /// <summary>
        /// ISO-8601 timestamp parsed to a friendly local display value
        /// (e.g. "07 Sep 2026 14:32"). Falls back to the raw value on parse failure.
        /// </summary>
        public string TimestampDisplay =>
            DateTime.TryParse(Entry.Timestamp, out var dt)
                ? dt.ToLocalTime().ToString("dd MMM yyyy HH:mm")
                : Entry.Timestamp;

        /// <summary>"Yes" / "No" for the Success column.</summary>
        public string SuccessText => Entry.IsSuccess == 1 ? "Yes" : "No";
    }
}