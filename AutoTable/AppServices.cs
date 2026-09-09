namespace AutoTable
{
    public static class AppServices
    {
        public static AutoTable.Services.IDataService? DataService { get; set; }
        // Toast notifications are enabled. Use AppServices.Toasts.Show(title, message) anywhere.
        public static AutoTable.Services.ToastService Toasts { get; } = AutoTable.Services.ToastService.Instance;

        /// <summary>
        /// Audit logging service. Use AppServices.Audit.LogAsync(entry) to record operations.
        /// </summary>
        public static AutoTable.Services.AuditService Audit { get; } = AutoTable.Services.AuditService.Instance;

        /// <summary>
        /// Blockchain licensing service for Ethereum Sepolia.
        /// </summary>
        public static AutoTable.Services.BlockchainLicenseService License { get; } = AutoTable.Services.BlockchainLicenseService.Instance;

        /// <summary>
        /// Key manager for blockchain key pair generation and signing.
        /// </summary>
        public static AutoTable.Services.KeyManager Key { get; } = AutoTable.Services.KeyManager.Instance;

        /// <summary>
        /// License period tracker for local license state and warnings.
        /// </summary>
        public static AutoTable.Services.LicensePeriodTracker LicenseTracker { get; } = AutoTable.Services.LicensePeriodTracker.Instance;

        /// <summary>
        /// SQLite connection string used by AuthService for user/invite-code lookups.
        /// Set during app startup in App.xaml.cs, before any auth view is navigated to.
        /// </summary>
        public static string? AuthConnectionString { get; set; }
    }
}
