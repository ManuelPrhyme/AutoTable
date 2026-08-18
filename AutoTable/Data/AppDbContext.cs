using AutoTable.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AutoTable.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<StudentEntity> Students => Set<StudentEntity>();
        public DbSet<ClassEntity> Classes => Set<ClassEntity>();
        public DbSet<StreamEntity> Streams => Set<StreamEntity>();
        public DbSet<SubjectEntity> Subjects => Set<SubjectEntity>();
        public DbSet<TermEntity> Terms => Set<TermEntity>();
        public DbSet<AcademicYearEntity> AcademicYears => Set<AcademicYearEntity>();
        public DbSet<AssessmentEntity> Assessments => Set<AssessmentEntity>();
        public DbSet<ClassSubjectEntity> ClassSubjects => Set<ClassSubjectEntity>();
        public DbSet<MarkEntity> Marks => Set<MarkEntity>();
        public DbSet<FeePaymentEntity> FeePayments => Set<FeePaymentEntity>();
        public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<TerminationLogEntity> TerminationLogs => Set<TerminationLogEntity>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Students
            modelBuilder.Entity<StudentEntity>()
                .HasIndex(s => s.LIN)
                .IsUnique();
            modelBuilder.Entity<StudentEntity>()
                .HasIndex(s => s.AdmissionNumber)
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

            // Relationships and cascade rules
            modelBuilder.Entity<AssessmentEntity>()
                .HasMany(a => a.Marks)
                .WithOne(m => m.Assessment)
                .HasForeignKey(m => m.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade); // deleting assessment removes marks

            modelBuilder.Entity<StudentEntity>()
                .HasMany(s => s.Marks)
                .WithOne(m => m.Student)
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // prevent accidental removal

            modelBuilder.Entity<StudentEntity>()
                .HasMany(s => s.FeePayments)
                .WithOne(fp => fp.Student)
                .HasForeignKey(fp => fp.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Termination log FK — preserve audit trail if student removed
            modelBuilder.Entity<TerminationLogEntity>()
                .HasOne(t => t.Student)
                .WithMany()
                .HasForeignKey(t => t.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TerminationLogEntity>()
                .HasIndex(t => t.StudentId);

            base.OnModelCreating(modelBuilder);
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
