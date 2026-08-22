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
                // Database file location: use persistent LocalApplicationData so data is retained between runs.
                var devDbFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoTable", "autotable.db");
                // Ensure directory exists
                try { var dbDir = System.IO.Path.GetDirectoryName(devDbFile); if (!string.IsNullOrEmpty(dbDir)) System.IO.Directory.CreateDirectory(dbDir); } catch { }
                string? dbError = null;

                try
                {
                    var sqliteConnection = AutoTable.Data.AppDbContext.CreateConnection(devDbFile);
                    var options = new DbContextOptionsBuilder<AutoTable.Data.AppDbContext>()
                        .UseSqlite(sqliteConnection)
                        .Options;

                    using (var ctx = new AutoTable.Data.AppDbContext(options))
                    {
                        // Create schema if missing. Do NOT delete or seed data by default — start with a plain DB.
                        ctx.Database.EnsureCreated();
                    }

                    // Best-effort compatibility fixes for older DB schemas
                    try
                    {
                        using var cmd = sqliteConnection.CreateCommand();
                        // Add Terms.EndDate column if missing
                        cmd.CommandText = "PRAGMA table_info('Terms');";
                        using var rdr = cmd.ExecuteReader();
                        var hasEnd = false;
                        while (rdr.Read())
                        {
                            if (string.Equals(rdr.GetString(1), "EndDate", StringComparison.OrdinalIgnoreCase)) { hasEnd = true; break; }
                        }
                        rdr.Close();
                        if (!hasEnd)
                        {
                            cmd.CommandText = "ALTER TABLE Terms ADD COLUMN EndDate TEXT;";
                            try { cmd.ExecuteNonQuery(); } catch { }
                        }

                        // Add Assessments.StreamId and Assessments.IsClassWide if missing
                        cmd.CommandText = "PRAGMA table_info('Assessments');";
                        using var r2 = cmd.ExecuteReader();
                        var hasStreamId = false; var hasIsClassWide = false;
                        while (r2.Read())
                        {
                            var col = r2.GetString(1);
                            if (string.Equals(col, "StreamId", StringComparison.OrdinalIgnoreCase)) hasStreamId = true;
                            if (string.Equals(col, "IsClassWide", StringComparison.OrdinalIgnoreCase)) hasIsClassWide = true;
                        }
                        r2.Close();
                        if (!hasStreamId)
                        {
                            cmd.CommandText = "ALTER TABLE Assessments ADD COLUMN StreamId INTEGER;";
                            try { cmd.ExecuteNonQuery(); } catch { }
                        }
                        if (!hasIsClassWide)
                        {
                            cmd.CommandText = "ALTER TABLE Assessments ADD COLUMN IsClassWide INTEGER DEFAULT 1;";
                            try { cmd.ExecuteNonQuery(); } catch { }
                        }
                    }
                    catch { }

                    // Ensure compatibility with older DBs: add missing Stream/ClassStreams table or Student.StreamId column if absent.
                    try
                    {
                        using var cmd = sqliteConnection.CreateCommand();
                        // Create ClassStreams join table if it doesn't exist
                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ClassStreams (
                                                ClassId INTEGER NOT NULL,
                                                StreamId INTEGER NOT NULL,
                                                PRIMARY KEY (ClassId, StreamId)
                                            );";
                        cmd.ExecuteNonQuery();

                        // Add StreamId column to Students if missing
                        cmd.CommandText = @"PRAGMA table_info('Students');";
                        using var reader = cmd.ExecuteReader();
                        var hasStreamId = false;
                        while (reader.Read())
                        {
                            var colName = reader.GetString(1);
                            if (string.Equals(colName, "StreamId", System.StringComparison.OrdinalIgnoreCase))
                            {
                                hasStreamId = true;
                                break;
                            }
                        }
                        reader.Close();

                        if (!hasStreamId)
                        {
                            cmd.CommandText = "ALTER TABLE Students ADD COLUMN StreamId INTEGER;";
                            try { cmd.ExecuteNonQuery(); } catch { /* best-effort */ }
                        }
                    }
                    catch { }

                    // Register the global data service.
                    AppServices.DataService = new DatabaseDataService(options);
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
