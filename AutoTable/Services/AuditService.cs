using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoTable.Data;
using AutoTable.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoTable.Services
{
    /// <summary>
    /// Provides audit logging, querying, and export capabilities.
    /// Every operation by every user is recorded for auditing and analytics.
    /// </summary>
    public sealed class AuditService
    {
        private static readonly Lazy<AuditService> _instance = new(() => new());
        public static AuditService Instance => _instance.Value;

        private string? _connectionString;

        private AuditService() { }

        public void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Log an operation. Fire-and-forget; never blocks the caller.
        /// </summary>
        public async Task LogAsync(AuditLogEntry entry)
        {
            try
            {
                if (string.IsNullOrEmpty(_connectionString)) return;

                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(_connectionString)
                    .Options;

                using var db = new AppDbContext(options);
                db.AuditLog.Add(entry);
                await db.SaveChangesAsync();
            }
            catch
            {
                // Audit logging must never break the operation that produced it.
            }
        }

        /// <summary>
        /// Convenience overload for common logging scenarios.
        /// </summary>
        public Task LogAsync(string category, string operation, string entityType,
            string? entityId, string? entityName, string? details = null,
            bool isSuccess = true, string? errorMessage = null)
        {
            var currentUser = SessionService.Instance.CurrentUser;
            var entry = new AuditLogEntry
            {
                Timestamp = DateTime.UtcNow.ToString("O"),
                UserId = currentUser?.UserId ?? 0,
                UserName = currentUser?.FullName ?? "System",
                UserEmail = currentUser?.Email,
                UserRole = currentUser?.Role.ToString(),
                Category = category,
                Operation = operation,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Details = details,
                IsSuccess = isSuccess ? 1 : 0,
                ErrorMessage = errorMessage,
                SessionId = null
            };
            return LogAsync(entry);
        }
        /// <summary>
        /// Get audit log entries with optional filtering.
        /// </summary>
        public async Task<List<AuditLogEntry>> GetEntriesAsync(
            string? category = null,
            string? operation = null,
            int? userId = null,
            string? entityType = null,
            string? entityId = null,
            bool? isSuccess = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? search = null,
            int limit = 50,
            int offset = 0)
        {
            if (string.IsNullOrEmpty(_connectionString)) return new List<AuditLogEntry>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connectionString)
                .Options;

            using var db = new AppDbContext(options);
            var query = db.AuditLog.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(e => e.Category == category);
            if (!string.IsNullOrEmpty(operation))
                query = query.Where(e => e.Operation == operation);
            if (userId.HasValue)
                query = query.Where(e => e.UserId == userId.Value);
            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(e => e.EntityType == entityType);
            if (!string.IsNullOrEmpty(entityId))
                query = query.Where(e => e.EntityId == entityId);
            if (isSuccess.HasValue)
                query = query.Where(e => e.IsSuccess == (isSuccess.Value ? 1 : 0));
            if (startDate.HasValue)
                query = query.Where(e => e.Timestamp.CompareTo(startDate.Value.ToString("O")) >= 0);
            if (endDate.HasValue)
                query = query.Where(e => e.Timestamp.CompareTo(endDate.Value.ToString("O")) <= 0);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(e =>
                    e.Timestamp.Contains(search) ||
                    e.UserId.ToString().Contains(search) ||
                    (e.UserName != null && e.UserName.Contains(search)) ||
                    (e.UserEmail != null && e.UserEmail.Contains(search)) ||
                    (e.UserRole != null && e.UserRole.Contains(search)) ||
                    e.Category.Contains(search) ||
                    e.Operation.Contains(search) ||
                    e.EntityType.Contains(search) ||
                    (e.EntityId != null && e.EntityId.Contains(search)) ||
                    (e.EntityName != null && e.EntityName.Contains(search)) ||
                    (e.Details != null && e.Details.Contains(search)) ||
                    (search.Equals("yes", StringComparison.OrdinalIgnoreCase) && e.IsSuccess == 1) ||
                    (search.Equals("no", StringComparison.OrdinalIgnoreCase) && e.IsSuccess == 0));

            return await query
                .OrderByDescending(e => e.Timestamp)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get all categories for filtering.
        /// </summary>
        public async Task<List<string>> GetCategoriesAsync()
        {
            if (string.IsNullOrEmpty(_connectionString)) return new List<string>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connectionString)
                .Options;

            using var db = new AppDbContext(options);
            return await db.AuditLog
                .Select(e => e.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        /// <summary>
        /// Get all operations for filtering.
        /// </summary>
        public async Task<List<string>> GetOperationsAsync()
        {
            if (string.IsNullOrEmpty(_connectionString)) return new List<string>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connectionString)
                .Options;

            using var db = new AppDbContext(options);
            return await db.AuditLog
                .Select(e => e.Operation)
                .Distinct()
                .OrderBy(o => o)
                .ToListAsync();
        }

        /// <summary>
        /// Get all users who have performed operations.
        /// </summary>
        public async Task<List<(int UserId, string UserName)>> GetUsersAsync()
        {
            if (string.IsNullOrEmpty(_connectionString)) return new List<(int, string)>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connectionString)
                .Options;

            using var db = new AppDbContext(options);
            var results = await db.AuditLog
                .Select(e => new { e.UserId, e.UserName })
                .Distinct()
                .OrderBy(e => e.UserName)
                .ToListAsync();

            return results.Select(e => (e.UserId, e.UserName)).ToList();
        }

        /// <summary>
        /// Get audit statistics for the KPI strip.
        /// </summary>
        public async Task<AuditStatistics> GetStatisticsAsync()
        {
            if (string.IsNullOrEmpty(_connectionString)) return new AuditStatistics();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connectionString)
                .Options;

            using var db = new AppDbContext(options);
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return new AuditStatistics
            {
                TotalOperations = await db.AuditLog.CountAsync(),
                OperationsToday = await db.AuditLog.CountAsync(e => e.Timestamp.StartsWith(today)),
                ActiveUsers = await db.AuditLog.Select(e => e.UserId).Distinct().CountAsync(),
                FailureCount = await db.AuditLog.CountAsync(e => e.IsSuccess == 0),
                MostCommonOperation = await db.AuditLog
                    .GroupBy(e => e.Operation)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync()
            };
        }

        /// <summary>
        /// Export filtered audit log entries to CSV.
        /// </summary>
        public async Task ExportToCsvAsync(string filePath,
            string? category = null,
            int? userId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var entries = await GetEntriesAsync(
                category: category,
                userId: userId,
                startDate: startDate,
                endDate: endDate,
                limit: 10000);

            var lines = new List<string>
            {
                "Timestamp,User,Role,Category,Operation,Entity,EntityName,Success,ErrorMessage,Details"
            };

            foreach (var e in entries)
            {
                lines.Add($"{e.Timestamp},{EscapeCsv(e.UserName)},{e.UserRole},{e.Category},{e.Operation},{e.EntityType},{EscapeCsv(e.EntityName)},{e.IsSuccess},{EscapeCsv(e.ErrorMessage)},{EscapeCsv(e.Details)}");
            }

            await File.WriteAllLinesAsync(filePath, lines);
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }

    public class AuditStatistics
    {
        public int TotalOperations { get; set; }
        public int OperationsToday { get; set; }
        public int ActiveUsers { get; set; }
        public int FailureCount { get; set; }
        public string? MostCommonOperation { get; set; }
    }
}
