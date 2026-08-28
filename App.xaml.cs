using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using AutoTable.Data;
using AutoTable.Demo;
using AutoTable.Services;
using Microsoft.EntityFrameworkCore;
using Velopack;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AutoTable
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        /// <summary>Public accessor for the main window (used by file pickers).</summary>
        public static Window? MainWindow => ((App)Current)._window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such it is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            // Global exception handlers to capture runtime errors during startup and at runtime
            this.UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // Support developer flags via environment variables:
                // - AUTOTABLE_DEV_EPHEMERAL_DB=true  -> use an ephemeral sqlite DB in Temp for dev/testing
                // - AUTOTABLE_DEMO_MODE=true         -> run in demo mode using MockDataServiceAdapter (no DB)
                static bool IsTruthy(string? v) => !string.IsNullOrWhiteSpace(v) && (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("yes", StringComparison.OrdinalIgnoreCase));

                var devEphemeral = IsTruthy(Environment.GetEnvironmentVariable("AUTOTABLE_DEV_EPHEMERAL_DB"));
                var demoMode = IsTruthy(Environment.GetEnvironmentVariable("AUTOTABLE_DEMO_MODE"));

                // Database file location: use persistent LocalApplicationData by default so data is retained between runs.
                var devDbFile = devEphemeral
                    ? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "autotable_ephemeral.db")
                    : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoTable", "autotable.db");
                // Ensure directory exists
                try { var dbDir = System.IO.Path.GetDirectoryName(devDbFile); if (!string.IsNullOrEmpty(dbDir)) System.IO.Directory.CreateDirectory(dbDir); } catch { }
                string? dbError = null;

                try
                {
                    if (demoMode)
                    {
                        // Demo mode: register the in-memory mock adapter and skip DB initialization
                        AppServices.DataService = new MockDataServiceAdapter();
                    }
                    else
                    {
                        // Migration connection for raw PRAGMA/ALTER commands at startup
                        var sqliteConnection = AutoTable.Data.AppDbContext.CreateConnection(devDbFile);

                        // EF options: use a CONNECTION STRING (not a shared connection object)
                        // so each DbContext gets its own connection.  The interceptor
                        // re-enables foreign_keys on every new connection.
                        var connStr = $"Data Source={devDbFile}";
                        var options = new DbContextOptionsBuilder<AutoTable.Data.AppDbContext>()
                            .UseSqlite(connStr)
                            .AddInterceptors(new AutoTable.Data.AppDbContext.ForeignKeyInterceptor())
                            .Options;

                        using (var ctx = new AutoTable.Data.AppDbContext(options))
                        {
                            // Create schema if missing. Do NOT delete or seed data by default — start with a plain DB.
                            ctx.Database.EnsureCreated();
                        }

                        // Apply all schema patches for legacy DB compatibility (idempotent)
                        SchemaPatches.ApplyAll(sqliteConnection);

                        // Before registering the data service, perform term activation housekeeping.
                        // NOTE: an active term stays active regardless of its end date — IsActive
                        // is a user-controlled setting, never auto-revoked by the calendar.
                        try
                        {
                            using var ctx = new AutoTable.Data.AppDbContext(options);
                            var now = DateTime.UtcNow;
                            var changed = false;

                            // Ensure there is exactly one active term ONLY if none is active:
                            // prefer term that contains 'now', else most recent by StartDate.
                            if (!ctx.Terms.AnyAsync(t => t.IsActive).GetAwaiter().GetResult())
                            {
                                var inRange = ctx.Terms.Where(t => t.StartDate.HasValue && t.EndDate.HasValue && t.StartDate.Value <= now && t.EndDate.Value >= now).OrderByDescending(t => t.StartDate).FirstOrDefaultAsync().GetAwaiter().GetResult();
                                if (inRange != null)
                                {
                                    inRange.IsActive = true;
                                    ctx.Terms.Update(inRange);
                                    changed = true;
                                }
                                else
                                {
                                    var latest = ctx.Terms.OrderByDescending(t => t.StartDate).FirstOrDefaultAsync().GetAwaiter().GetResult();
                                    if (latest != null)
                                    {
                                        latest.IsActive = true;
                                        ctx.Terms.Update(latest);
                                        changed = true;
                                    }
                                }
                            }

                            if (changed) ctx.SaveChangesAsync().GetAwaiter().GetResult();
                        }
                        catch { }

// Verify referential integrity: every Marks / FeePayments / Assessments row must point
                        // at a real student/term/etc. Violations are written to a temp report (best-effort).
                        try
                        {
                            var fkViolations = AutoTable.Data.AppDbContext.CheckReferentialIntegrity(sqliteConnection);
                            if (!string.IsNullOrWhiteSpace(fkViolations))
                            {
                                var fkPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "autotable_fk_report.txt");
                                System.IO.File.WriteAllText(fkPath, fkViolations);
                            }
                        }
                        catch { /* best-effort; a missed integrity check must never block startup */ }

                        // Register the global data service.
                        AppServices.DataService = new DatabaseDataService(options);
                    }
                }
                catch (Exception initEx)
                {
                    dbError = initEx.ToString();
                }

                // Surface DB init problems for diagnosis and fail early if DB cannot be used.
                if (dbError != null || AppServices.DataService == null)
                {
                    try
                    {
                        var diagPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "autotable_init_error.txt");
                        System.IO.File.WriteAllText(diagPath,
                            $"devDbFile={devDbFile}\r\nLOCALAPPDATA={Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\r\n{dbError}");
                    }
                    catch { }

                    // Show a minimal error window and abort startup so the app does not run with a missing data service.
                    var errWin = new Window();
                    var tb = new TextBlock
                    {
                        Text = "Failed to initialize database. See autotable_init_error.txt in your temp folder for details.\r\n" + (dbError ?? "DataService not configured."),
                        TextWrapping = TextWrapping.Wrap,
                        Padding = new Thickness(12)
                    };
                    errWin.Content = tb;
                    errWin.Activate();
                    return;
                }

                _window = new MainWindow();
                ThemeService.Initialize(_window);
                var root = new Frame();
                _window.Content = root;
                NavigationService.Instance.Initialize(root);
                root.Navigate(typeof(Views.LoginView));
                _window.Activate();
            }
            catch (Exception ex)
            {
                // Log and show a minimal window with the error so app doesn't exit silently
                try
                {
                    var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AutoTable_startup_error.txt");
                    System.IO.File.WriteAllText(path, ex.ToString());
                }
                catch { }

                var w = new Window();
                var tb = new TextBlock
                {
                    Text = "Failed to start application:\r\n" + ex.Message,
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(12)
                };
                w.Content = tb;
                w.Activate();
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AutoTable_unhandled_ui_exception.txt");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                sb.AppendLine($"Exception: {e.Exception.GetType().FullName}");
                sb.AppendLine($"Message: {e.Exception.Message}");
                sb.AppendLine($"HRESULT: 0x{e.Exception.HResult:X8}");
                sb.AppendLine();
                sb.AppendLine("Full stack:");
                sb.AppendLine(e.Exception.ToString());
                if (e.Exception.InnerException != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("--- Inner Exception ---");
                    sb.AppendLine(e.Exception.InnerException.ToString());
                }
                System.IO.File.WriteAllText(path, sb.ToString());
            }
            catch { }
            // Don't rethrow — keep the app alive for diagnostics
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            try
            {
                var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AutoTable_unhandled_exception.txt");
                System.IO.File.WriteAllText(path, (e.ExceptionObject as Exception)?.ToString() ?? "Unknown error");
            }
            catch { }
        }
    }
}
