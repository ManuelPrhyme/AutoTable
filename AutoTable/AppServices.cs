namespace AutoTable
{
    public static class AppServices
    {
                public static AutoTable.Services.IDataService? DataService { get; set; }
        // Toasts are commented out during development (dev mode).
        // public static AutoTable.Services.ToastService Toasts { get; } = AutoTable.Services.ToastService.Instance;
    }
}
