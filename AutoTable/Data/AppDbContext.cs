using AutoTable.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace AutoTable.Data
{
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// EF Core interceptor that enables <c>PRAGMA foreign_keys = ON</c> every time
        /// a new SQLite connection is opened.  This is required because the pragma is
        /// per-connection and EF Core opens a fresh connection for each DbContext.
        /// </summary>
        public sealed class ForeignKeyInterceptor : DbConnectionInterceptor
        {
            public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
            {
                try
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "PRAGMA foreign_keys = ON;";
                    cmd.ExecuteNonQuery();
                }
                catch { /* best-effort */ }
            }
        }

        public DbSet<StudentEntity> Students => Set<StudentEntity>();
        public DbSet<ClassEntity> Classes => Set<ClassEntity>();
        public DbSet<StreamEntity> Streams => Set<StreamEntity>();
        public DbSet<SubjectEntity> Subjects => Set<SubjectEntity>();
        public DbSet<TermEntity> Terms => Set<TermEntity>();
        public DbSet<AcademicYearEntity> AcademicYears => Set<AcademicYearEntity>();
        public DbSet<AssessmentEntity> Assessments => Set<AssessmentEntity>();
        public DbSet<AssessmentSubjectEntity> AssessmentSubjects => Set<AssessmentSubjectEntity>();
        public DbSet<ClassSubjectEntity> ClassSubjects => Set<ClassSubjectEntity>();
        public DbSet<MarkEntity> Marks => Set<MarkEntity>();
        public DbSet<FeePaymentEntity> FeePayments => Set<FeePaymentEntity>();
        public DbSet<StudentCreditEntity> StudentCredits => Set<StudentCreditEntity>();
        public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<TeacherEntity> Teachers => Set<TeacherEntity>();
        public DbSet<TerminationLogEntity> TerminationLogs => Set<TerminationLogEntity>();
        public DbSet<EnrollmentEntity> Enrollments => Set<EnrollmentEntity>();
        // New entities
        public DbSet<ClassStreamEntity> ClassStreams => Set<ClassStreamEntity>();
        public DbSet<TermFeeEntity> TermFees => Set<TermFeeEntity>();
        public DbSet<BudgetLineEntity> BudgetLines => Set<BudgetLineEntity>();

        // Grading system entities
        public DbSet<GradingSystemEntity> GradingSystems => Set<GradingSystemEntity>();
        public DbSet<GradeBandEntity> GradeBands => Set<GradeBandEntity>();
        public DbSet<SchoolSettingsEntity> SchoolSettings => Set<SchoolSettingsEntity>();
        public DbSet<HeadTeacherCommentEntity> HeadTeacherComments => Set<HeadTeacherCommentEntity>();
        public DbSet<InviteCodeEntity> InviteCodes => Set<InviteCodeEntity>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Students
            // Keep unique index on LIN (user-provided identifier)
            modelBuilder.Entity<StudentEntity>()
                .HasIndex(s => s.LIN)
                .IsUnique();

            // Class unique name
            modelBuilder.Entity<ClassEntity>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<StreamEntity>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<SubjectEntity>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<AcademicYearEntity>()
                .HasIndex(a => a.Name)
                .IsUnique();

            modelBuilder.Entity<TermEntity>()
                .HasIndex(t => t.Name)
                .IsUnique();

            // Assessments unique constraint for context
            modelBuilder.Entity<AssessmentEntity>()
                .HasIndex(a => new { a.Name, a.ClassId, a.SubjectId, a.TermId, a.AcademicYearId })
                .IsUnique();

            // Class-Subject many-to-many via join entity
            modelBuilder.Entity<ClassSubjectEntity>()
                .HasKey(cs => new { cs.ClassId, cs.SubjectId });

            // ClassStream many-to-many via join entity
            modelBuilder.Entity<ClassStreamEntity>()
                .HasKey(cs => new { cs.ClassId, cs.StreamId });

            modelBuilder.Entity<ClassStreamEntity>()
                .HasOne(cs => cs.Class)
                .WithMany(c => c.ClassStreams)
                .HasForeignKey(cs => cs.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassStreamEntity>()
                .HasOne(cs => cs.Stream)
                .WithMany(s => s.ClassStreams)
                .HasForeignKey(cs => cs.StreamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassStreamEntity>()
                .HasOne(cs => cs.StreamTeacher)
                .WithMany()
                .HasForeignKey(cs => cs.StreamTeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ClassSubjectEntity>()
                .HasOne(cs => cs.Class)
                .WithMany(c => c.ClassSubjects)
                .HasForeignKey(cs => cs.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassSubjectEntity>()
                .HasOne(cs => cs.Subject)
                .WithMany(s => s.ClassSubjects)
                .HasForeignKey(cs => cs.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Multi-subject assessment → subjects (many-to-many via AssessmentSubjects)
            modelBuilder.Entity<AssessmentSubjectEntity>()
                .HasKey(ass => new { ass.AssessmentId, ass.SubjectId });
            modelBuilder.Entity<AssessmentSubjectEntity>()
                .HasOne(ass => ass.Assessment)
                .WithMany(a => a.AssessmentSubjects)
                .HasForeignKey(ass => ass.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<AssessmentSubjectEntity>()
                .HasOne(ass => ass.Subject)
                .WithMany(s => s.AssessmentSubjects)
                .HasForeignKey(ass => ass.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mark → subject: null for single-subject marks
            modelBuilder.Entity<MarkEntity>()
                .HasOne(m => m.Subject)
                .WithMany(s => s.Marks)
                .HasForeignKey(m => m.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relationships and cascade rules
            modelBuilder.Entity<AssessmentEntity>()
                .HasMany(a => a.Marks)
                .WithOne(m => m.Assessment)
                .HasForeignKey(m => m.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade); // deleting assessment removes marks

            // Ensure Stream relationship for assessments
            modelBuilder.Entity<AssessmentEntity>()
                .HasOne(a => a.Stream)
                .WithMany()
                .HasForeignKey(a => a.StreamId)
                .OnDelete(DeleteBehavior.SetNull);

            // =========================================================================
            // Referential integrity (explicit)
            // Every Marks and FeePayments row must reference a real student. The
            // Students table is the single source of the student's name + LIN; mark/fee
            // rows never duplicate name/LIN text. SQLite enforces this via FK + PK.
            // =========================================================================
            modelBuilder.Entity<MarkEntity>()
                .HasOne(m => m.Student)
                .WithMany(s => s.Marks)
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // cannot orphan a mark; terminate the student instead
            modelBuilder.Entity<MarkEntity>()
                .HasIndex(m => m.StudentId);
            modelBuilder.Entity<MarkEntity>()
                .HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(m => m.EnteredByUserId)
                .OnDelete(DeleteBehavior.SetNull); // keep the mark if the teacher account is removed

            modelBuilder.Entity<FeePaymentEntity>()
                .HasOne(fp => fp.Student)
                .WithMany(s => s.FeePayments)
                .HasForeignKey(fp => fp.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // cannot orphan a payment
            modelBuilder.Entity<FeePaymentEntity>()
                .HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(fp => fp.RecordedByUserId)
                .OnDelete(DeleteBehavior.SetNull); // keep the payment if the officer account is removed
            modelBuilder.Entity<FeePaymentEntity>()
                .HasIndex(fp => new { fp.StudentId, fp.TermId });

            // Assessment obligations: term + academic year must exist before a paper can record marks
            modelBuilder.Entity<AssessmentEntity>()
                .HasOne(a => a.Term)
                .WithMany()
                .HasForeignKey(a => a.TermId)
                .OnDelete(DeleteBehavior.Restrict); // prevents deleting a term with assessments
            modelBuilder.Entity<AssessmentEntity>()
                .HasOne(a => a.AcademicYear)
                .WithMany()
                .HasForeignKey(a => a.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AssessmentEntity>()
                .HasIndex(a => a.TermId);

            // Class teacher must be a real row in the Teachers table
            modelBuilder.Entity<ClassEntity>()
                .HasOne(c => c.ClassTeacher)
                .WithMany()
                .HasForeignKey(c => c.ClassTeacherId)
                .OnDelete(DeleteBehavior.SetNull); // keep the class if the teacher leaves

            // Student promotion target class (Term 3 move-up): set to next class when promoted.
            modelBuilder.Entity<StudentEntity>()
                .HasOne(s => s.PromotedToClass)
                .WithMany()
                .HasForeignKey(s => s.PromotedToClassId)
                .OnDelete(DeleteBehavior.SetNull); // clear target if class is deleted

            // Termination log FK — preserve audit trail if student removed
            modelBuilder.Entity<TerminationLogEntity>()
                .HasOne(t => t.Student)
                .WithMany()
                .HasForeignKey(t => t.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TerminationLogEntity>()
                .HasIndex(t => t.StudentId);

            // Term fees: unique per term+class
            modelBuilder.Entity<TermFeeEntity>()
                .HasIndex(tf => new { tf.TermId, tf.ClassId })
                .IsUnique();

            // Budget lines: unique per category+year
            modelBuilder.Entity<BudgetLineEntity>()
                .HasIndex(b => new { b.Category, b.FinancialYear })
                .IsUnique();

            // InviteCode → CreatedByUser FK
            modelBuilder.Entity<InviteCodeEntity>()
                .HasOne(i => i.CreatedByUser)
                .WithMany()
                .HasForeignKey(i => i.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InviteCodeEntity>()
                .HasIndex(i => i.Code)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }

        // Helper to create a connection with foreign_keys ON (used by startup)
        /// <summary>
        /// Runs SQLite's <c>PRAGMA foreign_key_check</c> on the mark + fee tables. Any orphaned
        /// row (Marks/FeePayments pointing at a missing student, etc.) is returned as a line.
        /// An empty string means every mark and fee payment has a valid student reference.
        /// </summary>
        public static string CheckReferentialIntegrity(SqliteConnection conn)
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                foreach (var table in new[] { "Marks", "FeePayments", "Assessments" })
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"PRAGMA foreign_key_check('{table}');";
                    using var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        sb.AppendLine($"[{table}] FK violation: rowid={rdr.GetValue(1)} refs {rdr.GetValue(2)} (id {rdr.GetValue(3)}) -> {rdr.GetValue(4)}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                sb.AppendLine($"integrity check failed: {ex.Message}");
            }
            return sb.ToString().Trim();
        }

        // Helper to create a connection with foreign_keys ON (used by startup)
        public static SqliteConnection CreateConnection(string dataSourceFile)
        {
            var conn = new SqliteConnection($"Data Source={dataSourceFile}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
            return conn;
        }
    }
}
