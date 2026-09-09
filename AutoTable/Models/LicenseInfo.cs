using System;

namespace AutoTable.Models
{
    /// <summary>
    /// Represents the current license state of this AutoTable instance.
    /// Stored locally and updated from blockchain when online.
    /// </summary>
    public class LicenseInfo
    {
        /// <summary>Whether this instance has a valid license.</summary>
        public bool IsLicensed { get; set; }

        /// <summary>The activation code used (if any).</summary>
        public string? ActivationCode { get; set; }

        /// <summary>This instance's Ethereum address.</summary>
        public string? InstanceAddress { get; set; }

        /// <summary>When the license was activated.</summary>
        public DateTime? ActivatedAt { get; set; }

        /// <summary>When the license expires.</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>Days remaining until expiry.</summary>
        public int DaysRemaining => ExpiresAt.HasValue ? (int)(ExpiresAt.Value - DateTime.UtcNow).TotalDays : 0;

        /// <summary>Current license status.</summary>
        public LicenseStatus Status
        {
            get
            {
                if (!IsLicensed) return LicenseStatus.NotActivated;
                if (!ExpiresAt.HasValue) return LicenseStatus.Active;
                var days = DaysRemaining;
                if (days < 0) return LicenseStatus.Expired;
                if (days <= 7) return LicenseStatus.Critical;
                if (days <= 15) return LicenseStatus.ExpiringSoon;
                if (days <= 30) return LicenseStatus.Warning;
                return LicenseStatus.Active;
            }
        }

        /// <summary>Whether the grace period has expired (7 days after expiry).</summary>
        public bool GracePeriodExpired => ExpiresAt.HasValue && (DateTime.UtcNow - ExpiresAt.Value).TotalDays > 7;

        /// <summary>Whether full access should be granted.</summary>
        public bool HasFullAccess => Status == LicenseStatus.Active || Status == LicenseStatus.Warning || Status == LicenseStatus.ExpiringSoon;

        /// <summary>Whether read-only access should be granted (grace period).</summary>
        public bool HasReadOnlyAccess => Status == LicenseStatus.Critical || (Status == LicenseStatus.Expired && !GracePeriodExpired);
    }

    public enum LicenseStatus
    {
        NotActivated,
        Active,
        Warning,
        ExpiringSoon,
        Critical,
        Expired
    }
}
