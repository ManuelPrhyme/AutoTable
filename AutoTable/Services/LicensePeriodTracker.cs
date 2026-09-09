using System;
using System.IO;
using System.Text;
using System.Text.Json;
using AutoTable.Models;

namespace AutoTable.Services
{
    /// <summary>
    /// Tracks the license period locally and provides warning/access logic.
    /// Works offline using cached state with grace period.
    /// </summary>
    public class LicensePeriodTracker
    {
        private static readonly Lazy<LicensePeriodTracker> _instance = new(() => new LicensePeriodTracker());
        public static LicensePeriodTracker Instance => _instance.Value;

        private const string LicenseFileName = "autotable_license.dat";
        private LicenseInfo _license = new();

        public LicenseInfo License => _license;

        private LicensePeriodTracker() { }

        /// <summary>
        /// Load cached license state from disk.
        /// </summary>
        public void Initialize()
        {
            var path = GetLicensePath();
            if (!File.Exists(path)) return;

            try
            {
                var encrypted = File.ReadAllBytes(path);
                var decrypted = System.Security.Cryptography.ProtectedData.Unprotect(encrypted, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(decrypted);
                var loaded = JsonSerializer.Deserialize<LicenseInfo>(json);
                if (loaded != null) _license = loaded;
            }
            catch { /* Use default empty license */ }
        }

        /// <summary>
        /// Update license state after activation or blockchain sync.
        /// </summary>
        public void UpdateLicense(LicenseInfo info)
        {
            _license = info;
            SaveLicense();
        }

        /// <summary>
        /// Mark as activated with the given code and expiry.
        /// </summary>
        public void Activate(string code, DateTime expiresAt, string instanceAddress)
        {
            _license.IsLicensed = true;
            _license.ActivationCode = code;
            _license.InstanceAddress = instanceAddress;
            _license.ActivatedAt = DateTime.UtcNow;
            _license.ExpiresAt = expiresAt;
            SaveLicense();
        }

        /// <summary>
        /// Check if the license needs renewal warning.
        /// </summary>
        public bool ShouldShowWarning()
        {
            return _license.Status <= LicenseStatus.Warning;
        }

        /// <summary>
        /// Check if access should be blocked (expired + grace period over).
        /// </summary>
        public bool ShouldBlockAccess()
        {
            return _license.Status == LicenseStatus.Expired && _license.GracePeriodExpired;
        }

        /// <summary>
        /// Get a user-friendly status message.
        /// </summary>
        public string GetStatusMessage()
        {
            return _license.Status switch
            {
                LicenseStatus.NotActivated => "No license activated. Please enter an activation code.",
                LicenseStatus.Active => $"License active. {_license.DaysRemaining} days remaining.",
                LicenseStatus.Warning => $"License expires in {_license.DaysRemaining} days. Consider renewing soon.",
                LicenseStatus.ExpiringSoon => $"License expires in {_license.DaysRemaining} days. Please renew.",
                LicenseStatus.Critical => $"License expires in {_license.DaysRemaining} days! Renew now to avoid interruption.",
                LicenseStatus.Expired when !_license.GracePeriodExpired => $"License expired. Grace period: {(7 - (int)(DateTime.UtcNow - _license.ExpiresAt.Value).TotalDays)} days left.",
                LicenseStatus.Expired => "License expired. Please renew to continue using AutoTable.",
                _ => "Unknown license status."
            };
        }

        private string GetPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoTable");

        private string GetLicensePath() => Path.Combine(GetPath(), LicenseFileName);

        private void SaveLicense()
        {
            var dir = GetPath();
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_license);
            var bytes = Encoding.UTF8.GetBytes(json);
            var encrypted = System.Security.Cryptography.ProtectedData.Protect(bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            File.WriteAllBytes(GetLicensePath(), encrypted);
        }
    }
}
