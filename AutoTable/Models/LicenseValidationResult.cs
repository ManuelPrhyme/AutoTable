using System;

namespace AutoTable.Models
{
    /// <summary>
    /// Result of an offline license validation — carries the decision plus
    /// diagnostics so the UI can explain exactly why access was granted,
    /// restricted, or denied.
    /// </summary>
    public class LicenseValidationResult
    {
        /// <summary>Full access granted.</summary>
        public bool IsValid { get; set; }

        /// <summary>Read-only access during grace period.</summary>
        public bool IsGrace { get; set; }

        /// <summary>Blocked entirely (expired past grace, tampered, or not activated).</summary>
        public bool IsLocked => !IsValid && !IsGrace;

        /// <summary>True when the license file signature failed.</summary>
        public bool SignatureInvalid { get; set; }

        /// <summary>True when the monotonic counter regressed (tampering).</summary>
        public bool CounterRegression { get; set; }

        /// <summary>True when the system clock was moved before registration time.</summary>
        public bool ClockRollback { get; set; }

        /// <summary>True when there is no license file yet (first run before activation).</summary>
        public bool NotActivated { get; set; }

        /// <summary>Minutes remaining until expiry (negative when already expired).</summary>
        public long RemainingMinutes { get; set; }

        /// <summary>Minutes remaining in grace (0 when not in grace).</summary>
        public long GraceRemainingMinutes { get; set; }

        /// <summary>Human-readable summary for the UI/toast.</summary>
        public string Message { get; set; } = string.Empty;
    }
}