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
using AutoTable.Services;
using Microsoft.EntityFrameworkCore;

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
                // Development: use a fresh SQLite DB for each run (testing mode).
                // The DB is deleted, recreated, and seeded idempotently every startup.
                var devDbFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "autotable.db");
                string? dbError = null;

                // Remove stale database file from previous runs to avoid schema drift.
                if (System.IO.File.Exists(devDbFile))
                {
                    try { System.IO.File.Delete(devDbFile); } catch { }
                }

                try
                {
                    var sqliteConnection = AutoTable.Data.AppDbContext.CreateConnection(devDbFile);
                    var options = new DbContextOptionsBuilder<AutoTable.Data.AppDbContext>()
                        .UseSqlite(sqliteConnection)
                        .Options;

                    using (var ctx = new AutoTable.Data.AppDbContext(options))
                    {
                        ctx.Database.EnsureDeleted();
                        ctx.Database.EnsureCreated();
                        AutoTable.Data.SeedData.EnsureSeed(ctx);
                    }

                    // Register the global data service.
                    AppServices.DataService = new DatabaseDataService(options);
                }
                catch (Exception initEx)
                {
                    dbError = initEx.ToString();

                    // Ensure the UI service ALWAYS gets registered so page navigation never
                    // throws "DataService not configured", even if full recreate is not possible.
                    try
                    {
                        var fallbackConn = AppDbContext.CreateConnection(devDbFile);
                        var fallbackOptions = new DbContextOptionsBuilder<AppDbContext>()
                            .UseSqlite(fallbackConn)
                            .Options;
                        using (var ctx = new AppDbContext(fallbackOptions))
                        {
                            ctx.Database.EnsureDeleted();
                            ctx.Database.EnsureCreated();
                            AutoTable.Data.SeedData.EnsureSeed(ctx);
                        }

                        AppServices.DataService = new DatabaseDataService(fallbackOptions);
                    }
                    catch (Exception fallbackEx)
                    {
                        dbError = new AggregateException(initEx, fallbackEx).ToString();
                    }
                }

                // Surface DB init problems for diagnosis without breaking the UI shell.
                if (dbError != null)
                {
                    try
                    {
                        var diagPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "autotable_test_db_init_error.txt");
                        System.IO.File.WriteAllText(diagPath,
                            $"devDbFile={devDbFile}\r\nTEMP={System.IO.Path.GetTempPath()}\r\n{dbError}");
                    }
                    catch { }
                }

                // If both DB initialization attempts failed, register an in-memory mock
                // data service so the UI can still function for demos and diagnostics.
                if (AppServices.DataService == null)
                {
                    AppServices.DataService = new Services.MockDataServiceAdapter();
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
                System.IO.File.WriteAllText(path, e.Exception.ToString());
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
