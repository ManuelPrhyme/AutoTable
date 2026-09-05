using AutoTable.Data;
using AutoTable.Data.Entities;
using AutoTable.Models;
using AutoTable.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AutoTable.IntegrationTests
{
    /// <summary>
    /// xUnit collection definition to share the database fixture across tests.
    /// </summary>
    [CollectionDefinition("Database")]
    public class DatabaseCollectionDefinition : ICollectionFixture<DatabaseFixture> { }

    /// <summary>
    /// Shared test fixture that provides an in-memory SQLite database and a
    /// DatabaseDataService wired to it.  The connection is kept open for the
    /// lifetime of the fixture so the in-memory database doesn't get destroyed.
    /// </summary>
    public class DatabaseFixture : IDisposable
    {
        public SqliteConnection Connection { get; }
        public DbContextOptions<AppDbContext> Options { get; }
        public IDataService Service { get; }

        public DatabaseFixture()
        {
            Connection = new SqliteConnection("Data Source=:memory:");
            Connection.Open();

            // Enable foreign keys
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = ON;";
                cmd.ExecuteNonQuery();
            }

            Options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(Connection)
                .Options;

            // Create schema
            using (var ctx = new AppDbContext(Options))
            {
                ctx.Database.EnsureCreated();
            }

            Service = new DatabaseDataService(Options);
        }

        public void Dispose()
        {
            Connection.Dispose();
        }
    }

    /// <summary>
    /// Ensure a fresh database for each test class by sharing the fixture.
    /// </summary>
    [Collection("Database")]
    public class DatabaseDataServiceTests
    {
        private readonly DatabaseFixture _fixture;

        public DatabaseDataServiceTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        // ── Helper: seed a class, subject, term, and academic year ──

        private async Task SeedPrerequisitesAsync()
        {
            using var ctx = new AppDbContext(_fixture.Options);
            // Ensure all prerequisites exist regardless of what other tests seeded
            var cls = ctx.Classes.FirstOrDefault(c => c.Name == "P5");
            if (cls == null)
            {
                cls = new ClassEntity { Name = "P5" };
                ctx.Classes.Add(cls);
                await ctx.SaveChangesAsync();
            }
            var subj = ctx.Subjects.FirstOrDefault(s => s.Name == "Mathematics");
            if (subj == null)
            {
                subj = new SubjectEntity { Name = "Mathematics" };
                ctx.Subjects.Add(subj);
                await ctx.SaveChangesAsync();
            }
            if (!ctx.ClassSubjects.Any(cs => cs.ClassId == cls.Id && cs.SubjectId == subj.Id))
            {
                ctx.ClassSubjects.Add(new ClassSubjectEntity { ClassId = cls.Id, SubjectId = subj.Id });
                await ctx.SaveChangesAsync();
            }
            if (!ctx.Terms.Any(t => t.Name == "Term 1, 2025"))
            {
                ctx.Terms.Add(new TermEntity { Name = "Term 1, 2025", IsActive = true, StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 4, 30) });
                await ctx.SaveChangesAsync();
            }
            if (!ctx.AcademicYears.Any(y => y.Name == "2024-2025"))
            {
                ctx.AcademicYears.Add(new AcademicYearEntity { Name = "2024-2025" });
                await ctx.SaveChangesAsync();
            }
        }

        // ── Student CRUD ──

        [Fact]
        public async Task CreateStudent_ThenAppearsInList()
        {
            using var ctx = new AppDbContext(_fixture.Options);
            if (!ctx.Classes.Any())
            {
                ctx.Classes.Add(new ClassEntity { Name = "P1" });
                await ctx.SaveChangesAsync();
            }

            var student = await _fixture.Service.CreateStudentAsync(new Student
            {
                LIN = "TEST-001",
                FullName = "Test Student",
                ClassId = 1,
                Gender = "Male"
            });

            Assert.True(student.Id > 0);
            Assert.Equal("Test Student", student.FullName);

            var all = await _fixture.Service.GetStudentsAsync();
            Assert.Contains(all, s => s.LIN == "TEST-001");
        }

        [Fact]
        public async Task DuplicateLIN_Throws()
        {
            using var ctx = new AppDbContext(_fixture.Options);
            if (!ctx.Classes.Any())
            {
                ctx.Classes.Add(new ClassEntity { Name = "P1" });
                await ctx.SaveChangesAsync();
            }

            await _fixture.Service.CreateStudentAsync(new Student
            {
                LIN = "DUP-001",
                FullName = "First",
                ClassId = 1
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _fixture.Service.CreateStudentAsync(new Student
                {
                    LIN = "DUP-001",
                    FullName = "Second",
                    ClassId = 1
                }));
        }

        // ── Term lifecycle ──

        [Fact]
        public async Task CreateTerm_BecomesActive_OthersDeactivated()
        {
            await SeedPrerequisitesAsync();

            var t1 = await _fixture.Service.CreateTermAsync("T-Active-1");
            Assert.True(t1.Id > 0);

            // Create a second term — should become active, first deactivated
            var t2 = await _fixture.Service.CreateTermAsync("T-Active-2");
            Assert.True(t2.Id > 0);

            using var ctx = new AppDbContext(_fixture.Options);
            var activeTerms = await ctx.Terms.Where(t => t.IsActive).ToListAsync();
            Assert.Single(activeTerms);
            Assert.Equal("T-Active-2", activeTerms[0].Name);
        }

        // ── Assessment CRUD ──

        [Fact]
        public async Task CreateAssessment_ReturnsPopulatedItem()
        {
            await SeedPrerequisitesAsync();

            var item = await _fixture.Service.CreateAssessmentAsync(new AssessmentItem
            {
                Name = "Mid Term I",
                ClassName = "P5",
                Subject = "Mathematics",
                WeightPercent = 30,
                DueDate = new DateTime(2025, 5, 22)
            });

            Assert.True(int.Parse(item.Id) > 0);
            Assert.Equal("Mid Term I", item.Name);
            Assert.Equal("P5", item.ClassName);
            Assert.Equal(30, item.WeightPercent);
        }

        // ── Marks CRUD ──

        [Fact]
        public async Task UpdateMark_UpsertsAndRecomputesCompletion()
        {
            await SeedPrerequisitesAsync();

            // Create a student
            var student = await _fixture.Service.CreateStudentAsync(new Student
            {
                LIN = "MARK-001",
                FullName = "Mark Student",
                ClassId = 1
            });

            // Create an assessment
            var assessment = await _fixture.Service.CreateAssessmentAsync(new AssessmentItem
            {
                Name = "CAT 1",
                ClassName = "P5",
                Subject = "Mathematics",
                WeightPercent = 20,
                DueDate = DateTime.UtcNow
            });

            var assessId = int.Parse(assessment.Id);

            // Enter a mark
            await _fixture.Service.UpdateMarkAsync(assessId, student.Id, 75, "B", "Good work");

            // Verify via gradebook
            var gradebook = await _fixture.Service.GetGradebookAsync("P5", "Mathematics");
            Assert.Contains(gradebook, r => r.StudentName == "Mark Student" && r.Cat1 == 75);

            // Update the mark (upsert)
            await _fixture.Service.UpdateMarkAsync(assessId, student.Id, 82, "A", "Excellent");

            gradebook = await _fixture.Service.GetGradebookAsync("P5", "Mathematics");
            Assert.Contains(gradebook, r => r.StudentName == "Mark Student" && r.Cat1 == 82);
        }

        // ── Budget line CRUD ──

        [Fact]
        public async Task BudgetLine_CreateListUpdateDelete()
        {
            // Create
            var line = await _fixture.Service.CreateBudgetLineAsync(new BudgetLine
            {
                Category = "Teaching Salaries",
                Budgeted = 100_000_000,
                Spent = 50_000_000,
                FinancialYear = "2025"
            });

            Assert.True(line.Id > 0);
            Assert.Equal("Teaching Salaries", line.Category);

            // List
            var lines = await _fixture.Service.GetBudgetLinesAsync("2025");
            Assert.Single(lines);
            Assert.Equal(100_000_000m, lines[0].Budgeted);

            // Update
            line.Budgeted = 120_000_000;
            var updated = await _fixture.Service.UpdateBudgetLineAsync(line);
            Assert.NotNull(updated);
            Assert.Equal(120_000_000m, updated!.Budgeted);

            // Delete
            await _fixture.Service.DeleteBudgetLineAsync(line.Id);
            var afterDelete = await _fixture.Service.GetBudgetLinesAsync("2025");
            Assert.Empty(afterDelete);
        }

        [Fact]
        public async Task BudgetLine_DuplicateCategory_Throws()
        {
            await _fixture.Service.CreateBudgetLineAsync(new BudgetLine
            {
                Category = "Utilities",
                Budgeted = 5_000_000,
                FinancialYear = "2025"
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _fixture.Service.CreateBudgetLineAsync(new BudgetLine
                {
                    Category = "Utilities",
                    Budgeted = 6_000_000,
                    FinancialYear = "2025"
                }));
        }

        // ── Teacher CRUD ──

        [Fact]
        public async Task Teacher_CreateListDelete()
        {
            var teacher = await _fixture.Service.CreateTeacherAsync(new Teacher
            {
                FullName = "Ms. Test Teacher",
                Email = "test@school.com",
                Phone = "0700000000"
            });

            Assert.True(teacher.Id > 0);
            Assert.Equal("Ms. Test Teacher", teacher.FullName);

            var all = await _fixture.Service.GetTeachersAsync();
            Assert.Contains(all, t => t.FullName == "Ms. Test Teacher");

            await _fixture.Service.DeleteTeacherAsync(teacher.Id);
            all = await _fixture.Service.GetTeachersAsync();
            Assert.DoesNotContain(all, t => t.FullName == "Ms. Test Teacher");
        }

        // ── Fee payments ──

        [Fact]
        public async Task FeePayment_CreateAndRead()
        {
            using var ctx = new AppDbContext(_fixture.Options);
            if (!ctx.Classes.Any())
            {
                ctx.Classes.Add(new ClassEntity { Name = "P1" });
                await ctx.SaveChangesAsync();
            }

            var student = await _fixture.Service.CreateStudentAsync(new Student
            {
                LIN = "FEE-001",
                FullName = "Fee Student",
                ClassId = 1
            });

            await _fixture.Service.CreateFeePaymentAsync(student.Id, 500_000, description: "Tuition");

            var payments = await _fixture.Service.GetFeePaymentsAsync();
            Assert.Single(payments);
            Assert.Equal(500_000, payments[0].Amount);
            Assert.Equal("Fee Student", payments[0].StudentName);
        }
    }
}
