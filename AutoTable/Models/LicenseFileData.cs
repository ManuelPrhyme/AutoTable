using System;

namespace AutoTable.Models
{
    /// <summary>
    /// License file data model per the Licensing System Architecture.
    /// Combines calendar-time anchors with a monotonic counter so elapsed
    /// minutes are tamper-resistant and expiry is enforced at minute-level
    /// granularity — entirely offline.
    /// </summary>
    public class LicenseFileData
    {
        /// <summary>Unix timestamp (seconds) at activation.</summary>
        public long RegistrationTime { get; set; }

        /// <summary>Monotonic counter value at activation.</summary>
        public long RegistrationCounter { get; set; }

        /// <summary>Unix timestamp (seconds) of last successful validation.</summary>
        public long LastValidatedTime { get; set; }

        /// <summary>Monotonic counter value at last validation.</summary>
        public long LastValidatedCounter { get; set; }

        /// <summary>Total allowed minutes before expiry (assigned by vendor via activation code).</summary>
        public long LicensePeriodMinutes { get; set; }

        /// <summary>Grace period in minutes after expiry before full lock.</summary>
        public long GracePeriodMinutes { get; set; }

        /// <summary>This instance's Ethereum address (binds license to key pair).</summary>
        public string InstanceAddress { get; set; } = string.Empty;

        /// <summary>The activation code that was used (if any).</summary>
        public string? ActivationCode { get; set; }

        /// <summary>The school code assigned to this instance on-chain.</summary>
        public string? SchoolCode { get; set; }

        /// <summary>Hex-encoded ECDSA signature over the serialized fields above (field order matters — see EcdsaSigningService).</summary>
        public string? Signature { get; set; }

        /// <summary>Hard lock time = registration + (period + grace) in seconds.</summary>
        public long HardLockTimeSeconds => RegistrationTime + (LicensePeriodMinutes + GracePeriodMinutes) * 60L;

        /// <summary>Minutes elapsed since registration per UTC clock (offline).</summary>
        public long GetElapsedMinutesAt(long nowUtcSeconds) =>
            Math.Max(0, (nowUtcSeconds - RegistrationTime) / 60L);

        /// <summary>Expected monotonic counter value for the given elapsed minutes.</summary>
        public long ExpectedCounterAt(long nowUtcSeconds) =>
            RegistrationCounter + GetElapsedMinutesAt(nowUtcSeconds);
    }
}