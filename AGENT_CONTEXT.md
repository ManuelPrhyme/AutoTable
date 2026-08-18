# AutoTable - Agent Context Export

This file summarizes the current workspace state, recent changes, and run/setup instructions so another agent (or automation) can continue work.

> Paths in this document are relative to the repository root: `C:\Users\manue\Desktop\Desktop_Apps\AutoTable\`

---

## Environment

- OS/IDE: Microsoft Visual Studio Community 2026 (18.7.3)
- Project target: .NET 8
- Solution file: `AutoTable.slnx` (root: C:\Users\manue\Desktop\Desktop_Apps\AutoTable\AutoTable.slnx)
- Active branch: `sql_rec` (origin: https://github.com/ManuelPrhyme/AutoTable)
- Preferred shell for commands: PowerShell (powershell.exe)
 - UI framework: WinUI 3 (Windows App SDK) - this is a WinUI 3 desktop application

---

## High-level goal implemented

- Replace mock data with a SQLite-backed EF Core data layer for end-to-end CRUD testing.
- Add Student management (LIN unique identifier), termination semantics (soft-delete + anonymize option), transactional operations, and admin flows to create classes/subjects and assign subjects to classes.
- Database recreated on each app start for testing (fresh DB).

---

## Important files added or modified

Added files (new):

- `AutoTable/Models/StudentTerminationReason.cs` - enum for termination reasons.
- `AutoTable/Models/Student.cs` - Student DTO used by viewmodels/services.
- `AutoTable/Models/SimpleLookup.cs` - small Id/Name DTO for lists.
- `AutoTable/Data/Entities/StudentEntity.cs` - EF entities: Student, Class, Stream, Subject, Term, AcademicYear, Assessment, Mark, FeePayment, User, ClassSubjectEntity.
- `AutoTable/Data/AppDbContext.cs` - EF Core DbContext configuration, indices, FK rules, helper to create SqliteConnection with PRAGMA foreign_keys=ON.
- `AutoTable/Data/SeedData.cs` - seed logic for classes, subjects, students, assessments, marks and default class-subject assignments.
- `AutoTable/Services/IDataService.cs` - async service interface for app data operations (students, assessments, gradebook, class/subject management, transactional create/terminate).
- `AutoTable/Services/DatabaseDataService.cs` - implementation of IDataService using AppDbContext, includes transaction-based flows:
  - `CreateStudentWithInitialDataAsync(student, initialFeeAmount, initialMarks)` (atomic create + fees + marks)
  - `TerminateStudentAsync(studentId, reason, date, anonymize)` (atomic soft-delete + optional anonymize)
  - Class/Subject management: Create/Delete classes/subjects, Assign/Remove subject to/from class, GetSubjectsForClassAsync
- `AutoTable/AppServices.cs` - small static holder to register global IDataService instance at startup.
- `AutoTable/Views/StudentsView.xaml` and `.xaml.cs` - simple Students page skeleton.
- `AutoTable/ViewModels/StudentsViewModel.cs` - Students list and Add/Refresh/Terminate flows using AppServices.DataService.
- `AutoTable/Views/ClassesView.xaml` and `.xaml.cs` - admin UI skeleton to create classes/subjects and assign/remove subjects.
- `AutoTable/ViewModels/ClassesViewModel.cs` - viewmodel for classes UI.

Modified files:

- `App.xaml.cs` - added DB initialization code that creates an SQLite database file in the OS temp folder (`autotable_test.db`), enables PRAGMA foreign_keys, runs EnsureDeleted/EnsureCreated and seeding, and registers `AppServices.DataService = new DatabaseDataService(options)`. This produces a fresh DB each run (testing mode).

---

## EF Core / SQLite notes

- EF Core provider expected: `Microsoft.EntityFrameworkCore.Sqlite` (project must add this NuGet package). Also `Microsoft.EntityFrameworkCore.Design` recommended for migrations.
- AppDbContext.CreateConnection(dataSourceFile) opens a `Microsoft.Data.Sqlite.SqliteConnection` and runs `PRAGMA foreign_keys = ON;` so FK constraints are enforced on that connection.
- Many-to-many Class ↔ Subject implemented via `ClassSubjectEntity` join table (composite key ClassId+SubjectId).
- Indexes and unique constraints are configured for LIN (Students.LIN), AdmissionNumber, Class.Name, Subject.Name, Term.Name, AcademicYear.Name, and a unique composite index for Assessments in a given class/subject/term/year context.

---

## Transactional semantics

- All multi-step operations that must be atomic use EF Core transactions (BeginTransactionAsync / CommitAsync / RollbackAsync). Implemented methods:
  - `CreateStudentWithInitialDataAsync(student, initialFeeAmount, initialMarks)` — creates a student, optional initial fee payment, and optional marks in a single transaction.
  - `TerminateStudentAsync(studentId, reason, date, anonymize)` — sets IsActive=false and optionally clears PII; executed inside a transaction and rolled back on failure.
- Recommended pattern for other multi-step operations: use the same transaction pattern shown in `DatabaseDataService`.

---

## IDataService surface (high-level)

Key async methods implemented/available via `AppServices.DataService` after startup registration:

- Assessments / Gradebook / Marks
  - `GetAssessmentsAsync()`
  - `GetGradebookAsync(string className, string subject)`
  - `GetStudentMarksAsync(string className, string subject, string assessmentName)`

- Students
  - `GetStudentsAsync()`
  - `GetStudentByIdAsync(int id)`
  - `CreateStudentAsync(Student student)` (delegates to transactional create)
  - `CreateStudentWithInitialDataAsync(Student student, double? initialFeeAmount, IEnumerable<(int AssessmentId, double? Mark, string? Grade)>? initialMarks)`
  - `UpdateStudentAsync(Student student)`
  - `TerminateStudentAsync(int studentId, StudentTerminationReason reason, DateTime date, bool anonymize = false)`

- Class & Subject management
  - `GetClassesAsync()`
  - `CreateClassAsync(string name)`
  - `DeleteClassAsync(int classId)` (prevents deletion if students or assessments exist)
  - `GetSubjectsAsync()`
  - `CreateSubjectAsync(string name)`
  - `DeleteSubjectAsync(int subjectId)` (prevents deletion if assessments exist)
  - `AssignSubjectToClassAsync(int classId, int subjectId)`
  - `RemoveSubjectFromClassAsync(int classId, int subjectId)` (prevents removal if assessments exist)
  - `GetSubjectsForClassAsync(int classId)`

---

## Seed data

- Seed creates Classes P1..P7, Streams (A,B,C), Subjects (Math, English, Science, Social Studies, Religious Education), Academic years and Terms, two sample students (LIN-0001, LIN-0002), one admin user, one sample assessment in P5 Math, and two marks.
- Seed also assigns default subjects to P5 (Mathematics, English) using the ClassSubjects join table.

---

## How to run locally (agent instructions)

1. Ensure NuGet packages are installed for the AutoTable project:

   dotnet add .\AutoTable package Microsoft.EntityFrameworkCore.Sqlite

   dotnet add .\AutoTable package Microsoft.EntityFrameworkCore.Design

2. Build the solution:

   dotnet build .\AutoTable.slnx

3. Run (dev/test mode):

   - Launch from Visual Studio or run `dotnet run` for the AutoTable project. On startup the app will delete/create the test SQLite file (`autotable_test.db` in OS Temp) and seed it.
   - The database connection used by the app has PRAGMA foreign_keys = ON enabled.

4. To persist DB between runs: edit `App.xaml.cs` and **remove** the `EnsureDeleted()` call.

---

## Notes, caveats & recommended next steps

- You must restore/install EF Core SQLite packages before build succeeds; build errors seen if not installed.
- XAML compilation requires building in Visual Studio to generate InitializeComponent for pages. If pages report missing InitializeComponent errors, rebuild solution in Visual Studio.
- Many existing ViewModels still use `MockDataService.Instance`. Recommended: refactor these to use `AppServices.DataService` so the UI switches to the database-backed service.
- Add an audit/termination log table if you need an audit trail for termination events (recommended for production).
- For testing: use an in-memory SQLite connection (`DataSource=:memory:`) with the connection kept open and PRAGMA foreign_keys=ON; call `Database.EnsureCreated()` to initialize schema for tests.

---

## Quick file list (for agent automation)

- AutoTable.slnx (solution)
- AutoTable/App.xaml.cs (modified startup wiring)
- AutoTable/AppServices.cs (new)
- AutoTable/Models/Student.cs
- AutoTable/Models/StudentTerminationReason.cs
- AutoTable/Models/SimpleLookup.cs
- AutoTable/Data/AppDbContext.cs
- AutoTable/Data/SeedData.cs
- AutoTable/Data/Entities/StudentEntity.cs
- AutoTable/Services/IDataService.cs
- AutoTable/Services/DatabaseDataService.cs
- AutoTable/Views/StudentsView.xaml(.cs)
- AutoTable/ViewModels/StudentsViewModel.cs
- AutoTable/Views/ClassesView.xaml(.cs)
- AutoTable/ViewModels/ClassesViewModel.cs

---

If you want, I can:
- produce an automated script to install required NuGet packages and run a dev startup sequence,
- refactor existing ViewModels (Assessments, Gradebook, MarksEntry, Dashboard) to consume `AppServices.DataService`,
- add an audit/termination log entity and record termination events inside the same transaction.

End of context export.
