using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace AutoTable.Data
{
    /// <summary>
    /// Centralized schema patches for legacy database compatibility.
    /// Replaces the inline PRAGMA/ALTER logic that was scattered across App.xaml.cs.
    /// Each method is idempotent: it checks if a column/table exists before adding it.
    /// </summary>
    public static class SchemaPatches
    {
        /// <summary>
        /// Apply all schema patches to bring an older database up to date.
        /// Called once at startup after EnsureCreated().
        /// </summary>
        public static void ApplyAll(SqliteConnection connection)
        {
            PatchTerms(connection);
            PatchAssessments(connection);
            PatchStudents(connection);
            PatchUsers(connection);
            PatchClasses(connection);
            PatchFeePayments(connection);
            PatchClassStreams(connection);
            PatchStudentCredits(connection);
            PatchTermFees(connection);
            PatchBudgetLines(connection);
            PatchGradingSystems(connection);
            PatchSchoolSettings(connection);
            PatchHeadTeacherComments(connection);
            PatchMarks(connection);
            PatchAssessmentSubjects(connection);
            PatchAssessmentsNullableSubject(connection);
            PatchAssessmentsCreatedAt(connection);
            PatchAssessmentsStreamIds(connection);
        }

        // ── Terms ──────────────────────────────────────────────────────────
        private static void PatchTerms(SqliteConnection conn)
        {
            AddColumnIfMissing(conn, "Terms", "StartDate", "ALTER TABLE Terms ADD COLUMN StartDate TEXT;");
            AddColumnIfMissing(conn, "Terms", "EndDate", "ALTER TABLE Terms ADD COLUMN EndDate TEXT;");
            AddColumnIfMissing(conn, "Terms", "IsActive", "ALTER TABLE Terms ADD COLUMN IsActive INTEGER DEFAULT 0;");
        }

        // ── Assessments ────────────────────────────────────────────────────
        private static void PatchAssessments(SqliteConnection conn)
        {
            AddColumnIfMissing(conn, "Assessments", "StreamId", "ALTER TABLE Assessments ADD COLUMN StreamId INTEGER;");
            AddColumnIfMissing(conn, "Assessments", "IsClassWide", "ALTER TABLE Assessments ADD COLUMN IsClassWide INTEGER DEFAULT 1;");
            AddColumnIfMissing(conn, "Assessments", "PromotionRole", "ALTER TABLE Assessments ADD COLUMN PromotionRole INTEGER DEFAULT 0;");
            AddColumnIfMissing(conn, "Assessments", "AuthorUserId", "ALTER TABLE Assessments ADD COLUMN AuthorUserId INTEGER;");
            AddColumnIfMissing(conn, "Assessments", "AuthorName", "ALTER TABLE Assessments ADD COLUMN AuthorName TEXT;");
        }

        // ── Students ───────────────────────────────────────────────────────
        private static void PatchStudents(SqliteConnection conn)
        {
            AddColumnIfMissing(conn, "Students", "StreamId", "ALTER TABLE Students ADD COLUMN StreamId INTEGER;");
            AddColumnIfMissing(conn, "Students", "IsActive", "ALTER TABLE Students ADD COLUMN IsActive INTEGER DEFAULT 1;");

            // Promotion columns
            AddColumnIfMissing(conn, "Students", "PromotionStatus", "ALTER TABLE Students ADD COLUMN PromotionStatus INTEGER DEFAULT 0;");
            AddColumnIfMissing(conn, "Students", "PromotedToClassId", "ALTER TABLE Students ADD COLUMN PromotedToClassId INTEGER;");
            AddColumnIfMissing(conn, "Students", "PromotionProcessedAt", "ALTER TABLE Students ADD COLUMN PromotionProcessedAt TEXT;");

            // Extended enrollment columns
            AddColumnIfMissing(conn, "Students", "AdmissionNumber", "ALTER TABLE Students ADD COLUMN AdmissionNumber TEXT;");
            AddColumnIfMissing(conn, "Students", "GuardianName", "ALTER TABLE Students ADD COLUMN GuardianName TEXT;");
            AddColumnIfMissing(conn, "Students", "GuardianRelationship", "ALTER TABLE Students ADD COLUMN GuardianRelationship TEXT;");
            AddColumnIfMissing(conn, "Students", "GuardianPhone", "ALTER TABLE Students ADD COLUMN GuardianPhone TEXT;");
            AddColumnIfMissing(conn, "Students", "GuardianEmail", "ALTER TABLE Students ADD COLUMN GuardianEmail TEXT;");
            AddColumnIfMissing(conn, "Students", "GuardianAddress", "ALTER TABLE Students ADD COLUMN GuardianAddress TEXT;");
            AddColumnIfMissing(conn, "Students", "HasCustodyDocuments", "ALTER TABLE Students ADD COLUMN HasCustodyDocuments INTEGER DEFAULT 0;");
            AddColumnIfMissing(conn, "Students", "ResidenceProofType", "ALTER TABLE Students ADD COLUMN ResidenceProofType TEXT;");
            AddColumnIfMissing(conn, "Students", "ResidenceDistrict", "ALTER TABLE Students ADD COLUMN ResidenceDistrict TEXT;");
            AddColumnIfMissing(conn, "Students", "ResidenceZone", "ALTER TABLE Students ADD COLUMN ResidenceZone TEXT;");
            AddColumnIfMissing(conn, "Students", "HasImmunizationCard", "ALTER TABLE Students ADD COLUMN HasImmunizationCard INTEGER DEFAULT 0;");
            AddColumnIfMissing(conn, "Students", "HasMedicalExamReport", "ALTER TABLE Students ADD COLUMN HasMedicalExamReport INTEGER DEFAULT 0;");
            AddColumnIfMissing(conn, "Students", "AllergiesOrConditions", "ALTER TABLE Students ADD COLUMN AllergiesOrConditions TEXT;");
            AddColumnIfMissing(conn, "Students", "HealthInsurance", "ALTER TABLE Students ADD COLUMN HealthInsurance TEXT;");
            AddColumnIfMissing(conn, "Students", "EmergencyName", "ALTER TABLE Students ADD COLUMN EmergencyName TEXT;");
            AddColumnIfMissing(conn, "Students", "EmergencyRelationship", "ALTER TABLE Students ADD COLUMN EmergencyRelationship TEXT;");
            AddColumnIfMissing(conn, "Students", "EmergencyPhone", "ALTER TABLE Students ADD COLUMN EmergencyPhone TEXT;");
            AddColumnIfMissing(conn, "Students", "AuthorizedPickupPerson", "ALTER TABLE Students ADD COLUMN AuthorizedPickupPerson TEXT;");
        }

        // ── Users (Teachers) ───────────────────────────────────────────────
        private static void PatchUsers(SqliteConnection conn)
        {
            AddColumnIfMissing(conn, "Users", "Phone", "ALTER TABLE Users ADD COLUMN Phone TEXT;");
            AddColumnIfMissing(conn, "Users", "SubjectsTaught", "ALTER TABLE Users ADD COLUMN SubjectsTaught TEXT;");
            AddColumnIfMissing(conn, "Users", "ClassesTaught", "ALTER TABLE Users ADD COLUMN ClassesTaught TEXT;");
            AddColumnIfMissing(conn, "Users", "NextOfKinName", "ALTER TABLE Users ADD COLUMN NextOfKinName TEXT;");
            AddColumnIfMissing(conn, "Users", "NextOfKinRelationship", "ALTER TABLE Users ADD COLUMN NextOfKinRelationship TEXT;");
            AddColumnIfMissing(conn, "Users", "NextOfKinPhone", "ALTER TABLE Users ADD COLUMN NextOfKinPhone TEXT;");
            AddColumnIfMissing(conn, "Users", "PreviousSchools", "ALTER TABLE Users ADD COLUMN PreviousSchools TEXT;");
            AddColumnIfMissing(conn, "Users", "IsRegisteredTeacher", "ALTER TABLE Users ADD COLUMN IsRegisteredTeacher INTEGER DEFAULT 0;");
            AddColumnIfMissing(conn, "Users", "IsStudentTeacher", "ALTER TABLE Users ADD COLUMN IsStudentTeacher INTEGER DEFAULT 0;");
        }

        // ── Classes ────────────────────────────────────────────────────────
        private static void PatchClasses(SqliteConnection conn)
        {
            AddColumnIfMissing(conn, "Classes", "ClassTeacherId", "ALTER TABLE Classes ADD COLUMN ClassTeacherId INTEGER;");
            AddColumnIfMissing(conn, "Classes", "GradingSystemId", "ALTER TABLE Classes ADD COLUMN GradingSystemId INTEGER;");
        }

        // ── FeePayments ────────────────────────────────────────────────────
        private static void PatchFeePayments(SqliteConnection conn)
        {
            AddColumnIfMissing(conn, "FeePayments", "TermId", "ALTER TABLE FeePayments ADD COLUMN TermId INTEGER;");
            AddColumnIfMissing(conn, "FeePayments", "RecordedByUserId", "ALTER TABLE FeePayments ADD COLUMN RecordedByUserId INTEGER;");
            AddColumnIfMissing(conn, "FeePayments", "Description", "ALTER TABLE FeePayments ADD COLUMN Description TEXT;");
        }

        // ── ClassStreams ───────────────────────────────────────────────────
        private static void PatchClassStreams(SqliteConnection conn)
        {
            CreateTableIfNotExists(conn, "ClassStreams", @"
                CREATE TABLE IF NOT EXISTS ClassStreams (
                    ClassId INTEGER NOT NULL,
                    StreamId INTEGER NOT NULL,
                    PRIMARY KEY (ClassId, StreamId)
                );");
            AddColumnIfMissing(conn, "ClassStreams", "StreamTeacherId", "ALTER TABLE ClassStreams ADD COLUMN StreamTeacherId INTEGER;");
        }

        // ── StudentCredits ─────────────────────────────────────────────────
        private static void PatchStudentCredits(SqliteConnection conn)
        {
            CreateTableIfNotExists(conn, "StudentCredits", @"
                CREATE TABLE IF NOT EXISTS StudentCredits (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentId INTEGER NOT NULL,
                    FromTermId INTEGER NOT NULL,
                    AppliedToTermId INTEGER,
                    Amount REAL NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    AppliedAt TEXT,
                    Description TEXT,
                    FOREIGN KEY (StudentId) REFERENCES Students(Id),
                    FOREIGN KEY (FromTermId) REFERENCES Terms(Id),
                    FOREIGN KEY (AppliedToTermId) REFERENCES Terms(Id)
                );");
        }

        // ── TermFees ───────────────────────────────────────────────────────
        private static void PatchTermFees(SqliteConnection conn)
        {
            if (!TableExists(conn, "TermFees"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE TermFees (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TermId INTEGER NOT NULL,
                        ClassId INTEGER NOT NULL,
                        Amount REAL NOT NULL DEFAULT 0,
                        FOREIGN KEY(TermId) REFERENCES Terms(Id) ON DELETE RESTRICT,
                        FOREIGN KEY(ClassId) REFERENCES Classes(Id) ON DELETE CASCADE
                    );";
                cmd.ExecuteNonQuery();
                try
                {
                    cmd.CommandText = "CREATE UNIQUE INDEX IX_TermFees_TermId_ClassId ON TermFees(TermId, ClassId);";
                    cmd.ExecuteNonQuery();
                }
                catch { /* index may already exist */ }
            }
        }

        // ── BudgetLines ────────────────────────────────────────────────────
        private static void PatchBudgetLines(SqliteConnection conn)
        {
            if (!TableExists(conn, "BudgetLines"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE BudgetLines (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Category TEXT NOT NULL,
                        Budgeted REAL NOT NULL DEFAULT 0,
                        Spent REAL NOT NULL DEFAULT 0,
                        FinancialYear TEXT NOT NULL DEFAULT '',
                        CreatedAt TEXT NOT NULL DEFAULT ''
                    );";
                cmd.ExecuteNonQuery();
                try
                {
                    cmd.CommandText = "CREATE UNIQUE INDEX IX_BudgetLines_Category_FinancialYear ON BudgetLines(Category, FinancialYear);";
                    cmd.ExecuteNonQuery();
                }
                catch { /* index may already exist */ }
            }
        }

        // ── GradingSystems ─────────────────────────────────────────────────
        private static void PatchGradingSystems(SqliteConnection conn)
        {
            if (!TableExists(conn, "GradingSystems"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE GradingSystems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        IsDefault INTEGER NOT NULL DEFAULT 0,
                        PassMark REAL NOT NULL DEFAULT 50,
                        CreatedAt TEXT NOT NULL DEFAULT ''
                    );";
                cmd.ExecuteNonQuery();
                try
                {
                    cmd.CommandText = "CREATE UNIQUE INDEX IX_GradingSystems_Name ON GradingSystems(Name);";
                    cmd.ExecuteNonQuery();
                }
                catch { /* best-effort */ }
            }
            else
            {
                AddColumnIfMissing(conn, "GradingSystems", "PassMark", "ALTER TABLE GradingSystems ADD COLUMN PassMark REAL NOT NULL DEFAULT 50;");
            }

            if (!TableExists(conn, "GradeBands"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE GradeBands (
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
                cmd.ExecuteNonQuery();
                try
                {
                    cmd.CommandText = "CREATE INDEX IX_GradeBands_GradingSystemId ON GradeBands(GradingSystemId);";
                    cmd.ExecuteNonQuery();
                }
                catch { /* best-effort */ }
            }
        }

        // ── SchoolSettings ─────────────────────────────────────────────────
        private static void PatchSchoolSettings(SqliteConnection conn)
        {
            if (!TableExists(conn, "SchoolSettings"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SchoolSettings (
                        Id INTEGER PRIMARY KEY DEFAULT 1,
                        SchoolName TEXT NOT NULL DEFAULT 'AutoTable Academy',
                        SchoolAddress TEXT DEFAULT '',
                        SchoolPhone TEXT DEFAULT '',
                        HeadTeacherName TEXT DEFAULT '',
                        Motto TEXT DEFAULT '',
                        LogoBytes BLOB
                    );";
                cmd.ExecuteNonQuery();
                // Ensure singleton row exists
                cmd.CommandText = "INSERT OR IGNORE INTO SchoolSettings (Id, SchoolName) VALUES (1, 'AutoTable Academy');";
                cmd.ExecuteNonQuery();
            }
            else
            {
                AddColumnIfMissing(conn, "SchoolSettings", "LogoBytes", "ALTER TABLE SchoolSettings ADD COLUMN LogoBytes BLOB;");
            }
        }

        // ── HeadTeacherComments ────────────────────────────────────────────
        private static void PatchHeadTeacherComments(SqliteConnection conn)
        {
            CreateTableIfNotExists(conn, "HeadTeacherComments", @"
                CREATE TABLE IF NOT EXISTS HeadTeacherComments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentId INTEGER NOT NULL,
                    TermId INTEGER,
                    Comment TEXT DEFAULT '',
                    HeadTeacherName TEXT DEFAULT '',
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (StudentId) REFERENCES Students(Id),
                    FOREIGN KEY (TermId) REFERENCES Terms(Id)
                );");
        }

        // ── Marks (multi-subject support) ──────────────────────────────────
        private static void PatchMarks(SqliteConnection conn)
        {
            // SubjectId is null for single-subject marks (resolved via Assessment.SubjectId);
            // set only for marks in multi-subject assessments.
            AddColumnIfMissing(conn, "Marks", "SubjectId", "ALTER TABLE Marks ADD COLUMN SubjectId INTEGER;");

            // One-time back-fill: every existing mark belongs to a single-subject
            // assessment, so stamp it with that assessment's subject.
            using (var backfill = conn.CreateCommand())
            {
                backfill.CommandText = @"
                    UPDATE Marks
                    SET SubjectId = (SELECT a.SubjectId FROM Assessments a WHERE a.Id = Marks.AssessmentId)
                    WHERE SubjectId IS NULL
                      AND EXISTS (SELECT 1 FROM Assessments a2 WHERE a2.Id = Marks.AssessmentId AND a2.SubjectId IS NOT NULL);";
                backfill.ExecuteNonQuery();
            }

            // School-wide flag for AllInSchool assessments (ClassId only anchors the row;
            // the paper applies to every class).
            AddColumnIfMissing(conn, "Assessments", "IsSchoolWide", "ALTER TABLE Assessments ADD COLUMN IsSchoolWide INTEGER NOT NULL DEFAULT 0;");
        }

        // ── AssessmentSubjects (multi-subject assessments) ──────────────────
        private static void PatchAssessmentSubjects(SqliteConnection conn)
        {
            CreateTableIfNotExists(conn, "AssessmentSubjects", @"
                CREATE TABLE IF NOT EXISTS AssessmentSubjects (
                    AssessmentId INTEGER NOT NULL,
                    SubjectId INTEGER NOT NULL,
                    PRIMARY KEY (AssessmentId, SubjectId),
                    FOREIGN KEY (AssessmentId) REFERENCES Assessments(Id) ON DELETE CASCADE,
                    FOREIGN KEY (SubjectId) REFERENCES Subjects(Id)
                );");
        }

        // ── Helpers ────────────────────────────────────────────────────────

        // ── Assessments.SubjectId nullable (multi-subject assessments) ────────────────
        // SQLite cannot drop a NOT NULL constraint via ALTER TABLE, so legacy databases
        // created before multi-subject assessments keep "SubjectId INTEGER NOT NULL".
        // Writing a multi-subject assessment (SubjectId = NULL) then fails with
        // "NOT NULL constraint failed: Assessments.SubjectId". The standard SQLite fix
        // is a full table rebuild: create the new schema, copy the rows, swap names.
        private static void PatchAssessmentsNullableSubject(SqliteConnection conn)
        {
            // Already nullable (fresh DB created from the current model)? Nothing to do.
            bool subjectIdNotNull = false;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info('Assessments');";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(1), "SubjectId", StringComparison.OrdinalIgnoreCase))
                        subjectIdNotNull = reader.GetInt64(3) != 0; // "notnull" flag
                }
            }
            if (!subjectIdNotNull) return;

            using (var fkOff = conn.CreateCommand())
                fkOff.CommandText = "PRAGMA foreign_keys = OFF;";

            using (var tx = (SqliteTransaction)conn.BeginTransaction())
            {
                try
                {
                    Exec(conn, tx, @"
                        CREATE TABLE Assessments_new (
                            Id INTEGER NOT NULL CONSTRAINT PK_Assessments PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            ClassId INTEGER NOT NULL,
                            SubjectId INTEGER NULL,
                            AcademicYearId INTEGER NOT NULL,
                            TermId INTEGER NOT NULL,
                            WeightPercent INTEGER NOT NULL,
                            DueDate TEXT NULL,
                            IsVerified INTEGER NOT NULL,
                            IsPublished INTEGER NOT NULL,
                            MarksEnteredPercent INTEGER NOT NULL,
                            StreamId INTEGER NULL,
                            IsClassWide INTEGER NOT NULL DEFAULT 1,
                            PromotionRole INTEGER NOT NULL DEFAULT 0,
                            AuthorUserId INTEGER NULL,
                            AuthorName TEXT NULL,
                            IsSchoolWide INTEGER NOT NULL DEFAULT 0,
                            CONSTRAINT FK_Assessments_AcademicYears_AcademicYearId FOREIGN KEY (AcademicYearId) REFERENCES AcademicYears (Id) ON DELETE CASCADE,
                            CONSTRAINT FK_Assessments_Classes_ClassId FOREIGN KEY (ClassId) REFERENCES Classes (Id) ON DELETE CASCADE,
                            CONSTRAINT FK_Assessments_Subjects_SubjectId FOREIGN KEY (SubjectId) REFERENCES Subjects (Id) ON DELETE CASCADE,
                            CONSTRAINT FK_Assessments_Terms_TermId FOREIGN KEY (TermId) REFERENCES Terms (Id) ON DELETE CASCADE
                        );");
                    Exec(conn, tx, @"
                        INSERT INTO Assessments_new
                            (Id, Name, ClassId, SubjectId, AcademicYearId, TermId, WeightPercent, DueDate,
                             IsVerified, IsPublished, MarksEnteredPercent, StreamId, IsClassWide,
                             PromotionRole, AuthorUserId, AuthorName, IsSchoolWide)
                        SELECT
                            Id, Name, ClassId, SubjectId, AcademicYearId, TermId, WeightPercent, DueDate,
                            IsVerified, IsPublished, MarksEnteredPercent, StreamId, IsClassWide,
                            PromotionRole, AuthorUserId, AuthorName, IsSchoolWide
                        FROM Assessments;");
                    Exec(conn, tx, "DROP TABLE Assessments;");
                    Exec(conn, tx, "ALTER TABLE Assessments_new RENAME TO Assessments;");
                    // Indexes are dropped together with the old table — recreate them.
                    Exec(conn, tx, "CREATE INDEX IF NOT EXISTS IX_Assessments_AcademicYearId ON Assessments (AcademicYearId);");
                    Exec(conn, tx, "CREATE INDEX IF NOT EXISTS IX_Assessments_ClassId ON Assessments (ClassId);");
                    Exec(conn, tx, "CREATE UNIQUE INDEX IF NOT EXISTS IX_Assessments_Name_ClassId_SubjectId_StreamId_TermId_AcademicYearId ON Assessments (Name, ClassId, SubjectId, StreamId, TermId, AcademicYearId);");
                    // AUTOINCREMENT sequence: the old sqlite_sequence row died with the table.
                    Exec(conn, tx, "UPDATE sqlite_sequence SET seq = (SELECT COALESCE(MAX(Id), 0) FROM Assessments) WHERE name = 'Assessments';");
                    Exec(conn, tx, @"
                        INSERT INTO sqlite_sequence (name, seq)
                            SELECT 'Assessments', (SELECT COALESCE(MAX(Id), 0) FROM Assessments)
                            WHERE NOT EXISTS (SELECT 1 FROM sqlite_sequence WHERE name = 'Assessments');");
                    tx.Commit();
                }
                catch
                {
                    try { tx.Rollback(); } catch { }
                    throw;
                }
            }

            using (var fkOn = conn.CreateCommand())
                fkOn.CommandText = "PRAGMA foreign_keys = ON;";
        }

        private static void Exec(SqliteConnection conn, SqliteTransaction tx, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        // ── Assessments.CreatedAt (creation-date sorting) ─────────────────────────────
        // Existing DBs (and DBs rebuilt by PatchAssessmentsNullableSubject) have no
        // CreatedAt column. Add it and back-fill existing rows — best available proxy is
        // each row's DueDate; rows without a due date get a conservative old timestamp so
        // they sort as the oldest (which they are, being created before this feature).
        private static void PatchAssessmentsCreatedAt(SqliteConnection conn)
        {
            AddColumnIfMissing(conn, "Assessments", "CreatedAt", "ALTER TABLE Assessments ADD COLUMN CreatedAt TEXT;");
            using (var backfill = conn.CreateCommand())
            {
                backfill.CommandText = @"
                    UPDATE Assessments SET CreatedAt = DueDate
                    WHERE CreatedAt IS NULL AND DueDate IS NOT NULL;
                    UPDATE Assessments SET CreatedAt = '2000-01-01T00:00:00'
                    WHERE CreatedAt IS NULL;";
                backfill.ExecuteNonQuery();
            }
        }

        // ── Assessments.StreamIdsCsv (multi-stream assessments) ───────────────────────
        // Adds the column used to persist multiple target streams ("1,2,3") on a single
        // assessment. Existing single-stream rows keep using StreamId (which stays set
        // to the first stream), so no back-fill is needed.
        private static void PatchAssessmentsStreamIds(SqliteConnection conn)
        {
            AddColumnIfMissing(conn, "Assessments", "StreamIdsCsv", "ALTER TABLE Assessments ADD COLUMN StreamIdsCsv TEXT;");
        }

        private static bool TableExists(SqliteConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@name;";
            cmd.Parameters.AddWithValue("@name", tableName);
            return cmd.ExecuteScalar() != null;
        }

        private static bool ColumnExists(SqliteConnection conn, string tableName, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info('{tableName}');";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void AddColumnIfMissing(SqliteConnection conn, string tableName, string columnName, string ddl)
        {
            try
            {
                if (!ColumnExists(conn, tableName, columnName))
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = ddl;
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* best-effort: column may already exist or table may not exist yet */ }
        }

        private static void CreateTableIfNotExists(SqliteConnection conn, string tableName, string createDdl)
        {
            try
            {
                if (!TableExists(conn, tableName))
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = createDdl;
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* best-effort */ }
        }
    }
}
