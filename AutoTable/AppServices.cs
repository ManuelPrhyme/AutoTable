namespace AutoTable
{
    public static class AppServices
    {
                public static AutoTable.Services.IDataService? DataService { get; set; }
        // Toasts are commented out during development (dev mode).
        // public static AutoTable.Services.ToastService Toasts { get; } = AutoTable.Services.ToastService.Instance;

        /// <summary>
        /// SQLite connection string used by AuthService for user/invite-code lookups.
        /// Set during app startup in App.xaml.cs, before any auth view is navigated to.
        /// </summary>
        public static string? AuthConnectionString { get; set; }
    }
}
