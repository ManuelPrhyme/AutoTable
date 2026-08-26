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

                        // Best-effort compatibility fixes for older DB schemas
                        try
                        {
                            using var cmd = sqliteConnection.CreateCommand();
                        // Add Terms.StartDate/EndDate/IsActive columns if missing
                        cmd.CommandText = "PRAGMA table_info('Terms');";
                        using var rdr = cmd.ExecuteReader();
                        var hasStart = false;
                        var hasEnd = false;
                        var hasIsActive = false;
                        while (rdr.Read())
                        {
                            var col = rdr.GetString(1);
                            if (string.Equals(col, "StartDate", StringComparison.OrdinalIgnoreCase)) hasStart = true;
                            if (string.Equals(col, "EndDate", StringComparison.OrdinalIgnoreCase)) hasEnd = true;
                            if (string.Equals(col, "IsActive", StringComparison.OrdinalIgnoreCase)) hasIsActive = true;
                        }
                        rdr.Close();
                        if (!hasStart)
                        {
                            cmd.CommandText = "ALTER TABLE Terms ADD COLUMN StartDate TEXT;";
                            try { cmd.ExecuteNonQuery(); } catch { }
                        }
                        if (!hasEnd)
                        {
                            cmd.CommandText = "ALTER TABLE Terms ADD COLUMN EndDate TEXT;";
                            try { cmd.ExecuteNonQuery(); } catch { }
                        }
                        if (!hasIsActive)
                        {
                            cmd.CommandText = "ALTER TABLE Terms ADD COLUMN IsActive INTEGER DEFAULT 0;";
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
                            // Add PromotionRole column (0=None, 1=CountsTowardPromotion, 2=PromotionExam)
                            var hasPromotionRole = false;
                            using (var rPromo = cmd.ExecuteReader())
                            {
                                while (rPromo.Read())
                                {
                                    if (string.Equals(rPromo.GetString(1), "PromotionRole", StringComparison.OrdinalIgnoreCase))
                                    {
                                        hasPromotionRole = true;
                                        break;
                                    }
                                }
                            }
                            if (!hasPromotionRole)
                            {
                                cmd.CommandText = "ALTER TABLE Assessments ADD COLUMN PromotionRole INTEGER DEFAULT 0;";
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

                            // Add StreamTeacherId column to ClassStreams for legacy DBs.
                            try
                            {
                                using var checkCmd = sqliteConnection.CreateCommand();
                                checkCmd.CommandText = @"PRAGMA table_info('ClassStreams');";
                                var hasStreamTeacher = false;
                                using (var r = checkCmd.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        if (string.Equals(r.GetString(1), "StreamTeacherId", System.StringComparison.OrdinalIgnoreCase))
                                        {
                                            hasStreamTeacher = true;
                                            break;
                                        }
                                    }
                                }
                                if (!hasStreamTeacher)
                                {
                                    checkCmd.CommandText = "ALTER TABLE ClassStreams ADD COLUMN StreamTeacherId INTEGER;";
                                    try { checkCmd.ExecuteNonQuery(); } catch { /* best-effort */ }
                                }
                            }
                            catch { /* best-effort */ }

                        // Add StreamId and IsActive columns to Students if missing
                        cmd.CommandText = @"PRAGMA table_info('Students');";
                        var hasStreamId = false;
                        var hasIsActiveStudent = false;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var colName = reader.GetString(1);
                                if (string.Equals(colName, "StreamId", System.StringComparison.OrdinalIgnoreCase))
                                    hasStreamId = true;
                                if (string.Equals(colName, "IsActive", System.StringComparison.OrdinalIgnoreCase))
                                    hasIsActiveStudent = true;
                                if (hasStreamId && hasIsActiveStudent) break;
                            }
                        }

                        if (!hasStreamId)
                        {
                            cmd.CommandText = "ALTER TABLE Students ADD COLUMN StreamId INTEGER;";
                            try { cmd.ExecuteNonQuery(); } catch { /* best-effort */ }
                        }
                        if (!hasIsActiveStudent)
                        {
                            cmd.CommandText = "ALTER TABLE Students ADD COLUMN IsActive INTEGER DEFAULT 1;";
                            try { cmd.ExecuteNonQuery(); } catch { /* best-effort */ }
                        }

                        // Promotion columns (new feature)
                        // Uses its own command to avoid conflicts with the using-var reader above.
                        void AddStudentColumn(string col, string ddl)
                        {
                            using var checkCmd = sqliteConnection.CreateCommand();
                            checkCmd.CommandText = $"PRAGMA table_info('Students');";
                            using var r = checkCmd.ExecuteReader();
                            var found = false;
                            while (r.Read())
                            {
                                if (string.Equals(r.GetString(1), col, StringComparison.OrdinalIgnoreCase))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            r.Close();
                            if (!found)
                            {
                                checkCmd.CommandText = ddl;
                                try { checkCmd.ExecuteNonQuery(); } catch { /* best-effort */ }
                            }
                        }
                        AddStudentColumn("PromotionStatus", "ALTER TABLE Students ADD COLUMN PromotionStatus INTEGER DEFAULT 0;");
                        AddStudentColumn("PromotedToClassId", "ALTER TABLE Students ADD COLUMN PromotedToClassId INTEGER;");
                        AddStudentColumn("PromotionProcessedAt", "ALTER TABLE Students ADD COLUMN PromotionProcessedAt TEXT;");

                        // Extended enrollment columns (may be missing from older DBs)
                        AddStudentColumn("AdmissionNumber", "ALTER TABLE Students ADD COLUMN AdmissionNumber TEXT;");
                        AddStudentColumn("GuardianName", "ALTER TABLE Students ADD COLUMN GuardianName TEXT;");
                        AddStudentColumn("GuardianRelationship", "ALTER TABLE Students ADD COLUMN GuardianRelationship TEXT;");
                        AddStudentColumn("GuardianPhone", "ALTER TABLE Students ADD COLUMN GuardianPhone TEXT;");
                        AddStudentColumn("GuardianEmail", "ALTER TABLE Students ADD COLUMN GuardianEmail TEXT;");
                        AddStudentColumn("GuardianAddress", "ALTER TABLE Students ADD COLUMN GuardianAddress TEXT;");
                        AddStudentColumn("HasCustodyDocuments", "ALTER TABLE Students ADD COLUMN HasCustodyDocuments INTEGER DEFAULT 0;");
                        AddStudentColumn("ResidenceProofType", "ALTER TABLE Students ADD COLUMN ResidenceProofType TEXT;");
                        AddStudentColumn("ResidenceDistrict", "ALTER TABLE Students ADD COLUMN ResidenceDistrict TEXT;");
                        AddStudentColumn("ResidenceZone", "ALTER TABLE Students ADD COLUMN ResidenceZone TEXT;");
                        AddStudentColumn("HasImmunizationCard", "ALTER TABLE Students ADD COLUMN HasImmunizationCard INTEGER DEFAULT 0;");
                        AddStudentColumn("HasMedicalExamReport", "ALTER TABLE Students ADD COLUMN HasMedicalExamReport INTEGER DEFAULT 0;");
                        AddStudentColumn("AllergiesOrConditions", "ALTER TABLE Students ADD COLUMN AllergiesOrConditions TEXT;");
                        AddStudentColumn("HealthInsurance", "ALTER TABLE Students ADD COLUMN HealthInsurance TEXT;");
                        AddStudentColumn("EmergencyName", "ALTER TABLE Students ADD COLUMN EmergencyName TEXT;");
                        AddStudentColumn("EmergencyRelationship", "ALTER TABLE Students ADD COLUMN EmergencyRelationship TEXT;");
                        AddStudentColumn("EmergencyPhone", "ALTER TABLE Students ADD COLUMN EmergencyPhone TEXT;");
                        AddStudentColumn("AuthorizedPickupPerson", "ALTER TABLE Students ADD COLUMN AuthorizedPickupPerson TEXT;");
                        }
                        catch { }

                        // Ensure TermFees table exists for older DBs (created before TermFeeEntity was added)
                        try
                        {
                            using var cmdTF = sqliteConnection.CreateCommand();
                            cmdTF.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='TermFees';";
                            if (cmdTF.ExecuteScalar() == null)
                            {
                                cmdTF.CommandText = @"CREATE TABLE TermFees (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    TermId INTEGER NOT NULL,
                                    ClassId INTEGER NOT NULL,
                                    Amount REAL NOT NULL DEFAULT 0,
                                    FOREIGN KEY(TermId) REFERENCES Terms(Id) ON DELETE RESTRICT,
                                    FOREIGN KEY(ClassId) REFERENCES Classes(Id) ON DELETE CASCADE
                                );";
                                cmdTF.ExecuteNonQuery();
                                cmdTF.CommandText = "CREATE UNIQUE INDEX IX_TermFees_TermId_ClassId ON TermFees(TermId, ClassId);";
                                try { cmdTF.ExecuteNonQuery(); } catch { /* index may already exist */ }
                            }
                        }
                        catch { /* best-effort */ }

                        // Ensure compatibility with older DBs: add extended teacher profile columns to Users if absent.
                        try
                        {
                            using var cmdU = sqliteConnection.CreateCommand();
                            cmdU.CommandText = @"PRAGMA table_info('Users');";
                            var userCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            using (var rU = cmdU.ExecuteReader())
                            {
                                while (rU.Read()) userCols.Add(rU.GetString(1));
                            }

                            void AddUserColumn(string col, string ddl)
                            {
                                if (!userCols.Contains(col))
                                {
                                    cmdU.CommandText = ddl;
                                    try { cmdU.ExecuteNonQuery(); } catch { /* best-effort */ }
                                }
                            }

                            AddUserColumn("Phone", "ALTER TABLE Users ADD COLUMN Phone TEXT;");
                            AddUserColumn("SubjectsTaught", "ALTER TABLE Users ADD COLUMN SubjectsTaught TEXT;");
                            AddUserColumn("ClassesTaught", "ALTER TABLE Users ADD COLUMN ClassesTaught TEXT;");
                            AddUserColumn("NextOfKinName", "ALTER TABLE Users ADD COLUMN NextOfKinName TEXT;");
                            AddUserColumn("NextOfKinRelationship", "ALTER TABLE Users ADD COLUMN NextOfKinRelationship TEXT;");
                            AddUserColumn("NextOfKinPhone", "ALTER TABLE Users ADD COLUMN NextOfKinPhone TEXT;");
                            AddUserColumn("PreviousSchools", "ALTER TABLE Users ADD COLUMN PreviousSchools TEXT;");
                            AddUserColumn("IsRegisteredTeacher", "ALTER TABLE Users ADD COLUMN IsRegisteredTeacher INTEGER DEFAULT 0;");
                            AddUserColumn("IsStudentTeacher", "ALTER TABLE Users ADD COLUMN IsStudentTeacher INTEGER DEFAULT 0;");
                        }
                        catch { }

                        // Ensure compatibility with older DBs: add Classes.ClassTeacherId column if absent.
                        try
                        {
                            using var cmdC = sqliteConnection.CreateCommand();
                            cmdC.CommandText = @"PRAGMA table_info('Classes');";
                            var hasClassTeacherId = false;
                            using (var rC = cmdC.ExecuteReader())
                            {
                                while (rC.Read())
                                {
                                    if (string.Equals(rC.GetString(1), "ClassTeacherId", StringComparison.OrdinalIgnoreCase))
                                    {
                                        hasClassTeacherId = true;
                                        break;
                                    }
                                }
                            }
                            if (!hasClassTeacherId)
                            {
                                cmdC.CommandText = "ALTER TABLE Classes ADD COLUMN ClassTeacherId INTEGER;";
                                try { cmdC.ExecuteNonQuery(); } catch { /* best-effort */ }
                            }
                        }
                        catch { }

                        // Create BudgetLines table if missing (new table added after initial schema)
                        try
                        {
                            using var cmdB = sqliteConnection.CreateCommand();
                            cmdB.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='BudgetLines';";
                            var exists = cmdB.ExecuteScalar() != null;
                            if (!exists)
                            {
                                cmdB.CommandText = @"CREATE TABLE BudgetLines (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    Category TEXT NOT NULL,
                                    Budgeted REAL NOT NULL DEFAULT 0,
                                    Spent REAL NOT NULL DEFAULT 0,
                                    FinancialYear TEXT NOT NULL DEFAULT '',
                                    CreatedAt TEXT NOT NULL DEFAULT ''
                                );";
                                cmdB.ExecuteNonQuery();
                                // Add unique index on Category + FinancialYear
                                cmdB.CommandText = "CREATE UNIQUE INDEX IX_BudgetLines_Category_FinancialYear ON BudgetLines(Category, FinancialYear);";
                                try { cmdB.ExecuteNonQuery(); } catch { /* index may already exist */ }
                            }
                        }
                        catch { /* best-effort */ }

                        // Ensure older DBs have the current FeePayments columns. Legacy
                        // DB files predate TermId/RecordedByUserId/Description, so the
                        // EF insert in CreateFeePaymentAsync would throw DbUpdateException
                        // ("table FeePayments has no column named ...") — patch them here.
                        try
                        {
                            using var cmdF = sqliteConnection.CreateCommand();
                            cmdF.CommandText = @"PRAGMA table_info('FeePayments');";
                            var feeCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            using (var rF = cmdF.ExecuteReader())
                            {
                                while (rF.Read()) feeCols.Add(rF.GetString(1));
                            }

                            void AddFeeColumn(string col, string ddl)
                            {
                                if (!feeCols.Contains(col))
                                {
                                    cmdF.CommandText = ddl;
                                    try { cmdF.ExecuteNonQuery(); } catch { /* best-effort */ }
                                }
                            }

                            AddFeeColumn("TermId", "ALTER TABLE FeePayments ADD COLUMN TermId INTEGER;");
                            AddFeeColumn("RecordedByUserId", "ALTER TABLE FeePayments ADD COLUMN RecordedByUserId INTEGER;");
                            AddFeeColumn("Description", "ALTER TABLE FeePayments ADD COLUMN Description TEXT;");
                        }
                        catch { /* best-effort */ }

                        // ── Grading systems (new feature): create tables if missing and add
                        // the Classes.GradingSystemId column for databases created earlier.
                        try
                        {
                            using var cmdG = sqliteConnection.CreateCommand();
                            cmdG.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='GradingSystems';";
                            if (cmdG.ExecuteScalar() == null)
                            {
                                cmdG.CommandText = @"CREATE TABLE GradingSystems (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    Name TEXT NOT NULL,
                                    IsDefault INTEGER NOT NULL DEFAULT 0,
                                    PassMark REAL NOT NULL DEFAULT 50,
                                    CreatedAt TEXT NOT NULL DEFAULT ''
                                );";
                                cmdG.ExecuteNonQuery();
                                cmdG.CommandText = "CREATE UNIQUE INDEX IX_GradingSystems_Name ON GradingSystems(Name);";
                                try { cmdG.ExecuteNonQuery(); } catch { /* best-effort */ }
                            }
                            else
                            {
                                // Ensure PassMark column exists for legacy DB files
                                cmdG.CommandText = "PRAGMA table_info('GradingSystems');";
                                var hasPassMark = false;
                                using (var rPM = cmdG.ExecuteReader())
                                {
                                    while (rPM.Read())
                                    {
                                        if (string.Equals(rPM.GetString(1), "PassMark", StringComparison.OrdinalIgnoreCase))
                                        {
                                            hasPassMark = true;
                                            break;
                                        }
                                    }
                                }
                                if (!hasPassMark)
                                {
                                    cmdG.CommandText = "ALTER TABLE GradingSystems ADD COLUMN PassMark REAL NOT NULL DEFAULT 50;";
                                    try { cmdG.ExecuteNonQuery(); } catch { /* best-effort */ }
                                }
                            }

                            cmdG.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='GradeBands';";
                            if (cmdG.ExecuteScalar() == null)
                            {
                                cmdG.CommandText = @"CREATE TABLE GradeBands (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    GradingSystemId INTEGER NOT NULL,
                                    Label TEXT NOT NULL,
                                    MinScore REAL NOT NULL DEFAULT 0,
                                    MaxScore REAL NOT NULL DEFAULT 0,
                                    IsPromotionalPass INTEGER NOT NULL DEFAULT 0,
                                    IsRepeater INTEGER NOT NULL DEFAULT 0,
                                    IsPromotionalFail INTEGER NOT NULL DEFAULT 0,
                                    FOREIGN KEY(GradingSystemId) REFERENCES GradingSystems(Id) ON DELETE CASCADE
                                );";
                                cmdG.ExecuteNonQuery();
                                cmdG.CommandText = "CREATE INDEX IX_GradeBands_GradingSystemId ON GradeBands(GradingSystemId);";
                                try { cmdG.ExecuteNonQuery(); } catch { /* best-effort */ }
                            }

                            // Classes.GradingSystemId column for legacy DB files
                            using var cmdC2 = sqliteConnection.CreateCommand();
                            cmdC2.CommandText = @"PRAGMA table_info('Classes');";
                            var hasGradingSystemId = false;
                            using (var rC2 = cmdC2.ExecuteReader())
                            {
                                while (rC2.Read())
                                {
                                    if (string.Equals(rC2.GetString(1), "GradingSystemId", StringComparison.OrdinalIgnoreCase))
                                    {
                                        hasGradingSystemId = true;
                                        break;
                                    }
                                }
                            }
                            if (!hasGradingSystemId)
                            {
                                cmdC2.CommandText = "ALTER TABLE Classes ADD COLUMN GradingSystemId INTEGER;";
                                try { cmdC2.ExecuteNonQuery(); } catch { /* best-effort */ }
                            }
                        }
                        catch { /* best-effort */ }

                        // Before registering the data service, perform term activation housekeeping:
                        try
                        {
                            using var ctx = new AutoTable.Data.AppDbContext(options);
                            var now = DateTime.UtcNow;
                            var changed = false;

                            // Deactivate any active term that has ended
                            var endedActive = ctx.Terms.Where(t => t.IsActive && t.EndDate.HasValue && t.EndDate.Value < now).ToListAsync().GetAwaiter().GetResult();
                            foreach (var et in endedActive)
                            {
                                et.IsActive = false;
                                ctx.Terms.Update(et);
                                changed = true;
                            }

                            // Ensure there is exactly one active term: prefer term that contains 'now', else most recent by StartDate
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
