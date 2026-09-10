using System;
using System.IO;
using System.Text;
using System.Text.Json;
using AutoTable.Models;

namespace AutoTable.Services
{
    /// <summary>
    /// Core offline licensing orchestrator. Implements the Licensing System
    /// Architecture: AES-256-GCM confidentiality, ECDSA integrity, monotonic
    /// counter progression, UTC clock anchor, minute-level expiry and grace.
    /// </summary>
    public class LicenseManager
    {
        private static readonly Lazy<LicenseManager> _instance = new(() => new LicenseManager());
        public static LicenseManager Instance => _instance.Value;

        public const string LicenseFileName = "autotable_license.dat";

        private readonly object _lock = new();

        /// <summary>The loaded license file (null when not yet activated).</summary>
        public LicenseFileData? Current { get; private set; }

        private LicenseManager() { }

        public string GetLicensePath() =>
            Path.Combine(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoTable"),
                LicenseFileName);

        // ── Registration (after on-chain activation) ─────────────────────────

        /// <summary>
        /// Create the local license file after a successful blockchain activation.
        /// periodMinutes and graceMinutes come from the vendor's activation-code
        /// parameters returned by the contract.
        /// </summary>
        public void Activate(
            long periodMinutes,
            long graceMinutes,
            string activationCode,
            string schoolCode,
            string instanceAddress)
        {
            lock (_lock)
            {
                var key = KeyManager.Instance;
                if (string.IsNullOrEmpty(key.PrivateKey))
                    throw new InvalidOperationException("No key pair available.");

                var counter = MonotonicCounter.Instance;
                var regCounter = Math.Max(counter.Read(), ReadLastValidatedFromDisk()) + 1;
                counter.EnsureAtLeast(regCounter);

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var license = new LicenseFileData
                {
                    RegistrationTime = now,
                    RegistrationCounter = regCounter,
                    LastValidatedTime = now,
                    LastValidatedCounter = regCounter,
                    LicensePeriodMinutes = periodMinutes,
                    GracePeriodMinutes = graceMinutes,
                    InstanceAddress = instanceAddress,
                    ActivationCode = activationCode,
                    SchoolCode = schoolCode
                };

                license.Signature = EcdsaSigningService.Instance.Sign(
                    EcdsaSigningService.Instance.BuildSigningPayload(license),
                    key.PrivateKey);

                Current = license;
                Save(license);
            }
        }
// ── Offline validation (every startup) ───────────────────────────────

        /// <summary>
        /// Validate the local license entirely offline: verify signature, detect
        /// clock rollback / counter regression, reconcile the counter, decide
        /// Active / Grace / Locked.
        /// </summary>
        public LicenseValidationResult Validate()
        {
            lock (_lock)
            {
                LicenseFileData license;
                try
                {
                    license = Load();
                }
                catch
                {
                    return new LicenseValidationResult
                    {
                        NotActivated = true,
                        IsValid = false,
                        Message = "No valid license file found. Please activate your license."
                    };
                }

                Current = license;

                var key = KeyManager.Instance;
                var counter = MonotonicCounter.Instance;
                var result = new LicenseValidationResult();

                // 1. ECDSA signature integrity.
                var payload = EcdsaSigningService.Instance.BuildSigningPayload(license);
                if (!string.IsNullOrEmpty(key.PublicKey) &&
                    !EcdsaSigningService.Instance.Verify(payload, license.Signature ?? "", key.PublicKey))
                {
                    result.SignatureInvalid = true;
                    result.Message = "License file failed signature verification. The file has been tampered with.";
                    return result;
                }

                // 2. Current state from UTC clock + monotonic counter.
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var currentCounter = Math.Max(counter.Read(), 0);

                // 3. Clock rollback detection.
                if (now < license.RegistrationTime)
                {
                    result.ClockRollback = true;
                    result.Message = "System clock rollback detected. License cannot be validated.";
                    return result;
                }

                // 4. Elapsed minutes + expected counter.
                var elapsed = license.GetElapsedMinutesAt(now);
                var expected = license.RegistrationCounter + elapsed;

                // 5. Counter regression detection.
                if (currentCounter > expected + 5 /* small drift tolerance */)
                {
                    result.CounterRegression = true;
                    result.Message = "License counter regression detected. The license may have been tampered with.";
                    return result;
                }

                // 6. Reconcile: bring the counter to the expected value (soft catch-up).
                counter.EnsureAtLeast(expected);

                // 7. Expiry decision.
                var total = license.LicensePeriodMinutes + license.GracePeriodMinutes;
                var remainingToExpiry = license.LicensePeriodMinutes - elapsed;
                var remainingToHardLock = total - elapsed;

                if (elapsed <= license.LicensePeriodMinutes)
                {
                    result.IsValid = true;
                    result.RemainingMinutes = remainingToExpiry;
                    result.Message = remainingToExpiry <= 30 * 24 * 60
                        ? $"License will expire in about {remainingToExpiry / (24 * 60)} day(s)."
                        : "License is active.";
                }
                else if (elapsed <= total)
                {
                    result.IsGrace = true;
                    result.GraceRemainingMinutes = remainingToHardLock;
                    result.Message = $"License expired. Read-only grace period: {remainingToHardLock / (24 * 60)} day(s) remaining.";
                }
                else
                {
                    result.Message = "License expired and grace period has ended. Please renew.";
                }

                // 8. Persist last-validated values.
                license.LastValidatedTime = now;
                license.LastValidatedCounter = counter.Read();
                Save(license);

                return result;
            }
        }
// ── Persistence ──────────────────────────────────────────────────────

        private void Save(LicenseFileData license)
        {
            var key = KeyManager.Instance;
            if (string.IsNullOrEmpty(key.PrivateKey)) return;

            var json = JsonSerializer.Serialize(license);
            var plain = Encoding.UTF8.GetBytes(json);

            var aad = license.InstanceAddress + "|" + (license.SchoolCode ?? "");
            var encrypted = AesEncryptionService.Instance.Encrypt(plain, key.PrivateKey, aad);

            // Blob format: HDR:<instanceAddress>:<encrypted base64>
            var blob = HeaderMarker + ":" + license.InstanceAddress + ":" + encrypted;

            var dir = Path.GetDirectoryName(GetLicensePath());
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(GetLicensePath(), blob);
        }

        private LicenseFileData Load()
        {
            var key = KeyManager.Instance;
            if (string.IsNullOrEmpty(key.PrivateKey))
                throw new InvalidOperationException("No key pair available.");

            var path = GetLicensePath();
            if (!File.Exists(path))
                throw new FileNotFoundException("License file not found.");

            var blob = File.ReadAllText(path);
            var parts = blob.Split(':');
            if (parts.Length < 3 || parts[0] != HeaderMarker)
                throw new InvalidDataException("License file format invalid.");

            var instanceAddress = parts[1];
            var encrypted = string.Join(":", parts, 2, parts.Length - 2);

            var aad = instanceAddress + "|";
            var jsonBytes = AesEncryptionService.Instance.Decrypt(encrypted, key.PrivateKey, aad);
            var json = Encoding.UTF8.GetString(jsonBytes);

            var loaded = JsonSerializer.Deserialize<LicenseFileData>(json);
            if (loaded == null)
                throw new InvalidDataException("License file could not be deserialized.");

            return loaded;
        }

        private long ReadLastValidatedFromDisk()
        {
            try
            {
                var path = GetLicensePath();
                if (!File.Exists(path)) return 0;
                var text = File.ReadAllText(path);
                var parts = text.Split(':');
                if (parts.Length < 3 || parts[0] != HeaderMarker)
                    return 0;
                var instanceAddress = parts[1];
                var encrypted = string.Join(":", parts, 2, parts.Length - 2);
                var jsonBytes = AesEncryptionService.Instance.Decrypt(
                    encrypted, KeyManager.Instance.PrivateKey ?? "", instanceAddress + "|");
                var loaded = JsonSerializer.Deserialize<LicenseFileData>(Encoding.UTF8.GetString(jsonBytes));
                return loaded?.LastValidatedCounter ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private const string HeaderMarker = "HDR";

        /// <summary>Check whether a license file exists.</summary>
        public bool HasLicenseFile => File.Exists(GetLicensePath());
    }
}