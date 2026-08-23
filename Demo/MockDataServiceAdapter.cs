using AutoTable.Models;
using AutoTable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.Demo
{
    // Lightweight adapter that exposes the in-memory MockDataService via the IDataService interface.
    // Used as a safe fallback when the real database cannot be initialized.
    public class MockDataServiceAdapter : IDataService
    {
        private readonly MockDataService _mock = MockDataService.Instance;

        public Task<AssessmentItem> CreateAssessmentAsync(AssessmentItem item)
        {
            // Return the item as created (no persistence in mock)
            return Task.FromResult(item);
        }

        public Task<Student> CreateStudentAsync(Student student)
            => Task.FromResult(student);

        public Task<Student> CreateStudentWithInitialDataAsync(Student student, double? initialFeeAmount = null, IEnumerable<(int AssessmentId, double? Mark, string? Grade)>? initialMarks = null)
            => Task.FromResult(student);

        public Task DeleteClassAsync(int classId)
            => Task.CompletedTask;

        public Task AssignSubjectToClassAsync(int classId, int subjectId)
            => Task.CompletedTask;

        public Task RemoveSubjectFromClassAsync(int classId, int subjectId)
            => Task.CompletedTask;

        public Task<IReadOnlyList<TerminationLogItem>> GetTerminationLogAsync()
            => Task.FromResult<IReadOnlyList<TerminationLogItem>>(Array.Empty<TerminationLogItem>());

        public Task<IReadOnlyList<AssessmentItem>> GetAssessmentsAsync()
            => Task.FromResult<IReadOnlyList<AssessmentItem>>(_mock.GetAssessments());

        public Task<AssessmentItem?> GetAssessmentAsync(string name, string className, string subject)
        {
            var found = _mock.GetAssessments().FirstOrDefault(a => a.Name == name && a.ClassName == className && a.Subject == subject);
            return Task.FromResult<AssessmentItem?>(found);
        }

        public Task<IReadOnlyList<Models.GradebookRow>> GetGradebookAsync(string className, string subject, string? academicYear = null, string? term = null, string? stream = null, string? studentName = null)
        {
            var rows = _mock.GetGradebook(className, subject, academicYear, term, stream, studentName);
            return Task.FromResult<IReadOnlyList<Models.GradebookRow>>(rows);
        }

        public Task<IReadOnlyList<Models.StudentMarkRow>> GetStudentMarksAsync(string className, string subject, string assessmentName)
        {
            var rows = _mock.GetStudentMarks(className, subject, assessmentName);
            return Task.FromResult<IReadOnlyList<Models.StudentMarkRow>>(rows);
        }

        public Task UpdateMarkAsync(int assessmentId, int studentId, double? mark, string? grade, string? remarks = null)
        {
            // Mock: no-op
            return Task.CompletedTask;
        }

        public Task DeleteMarkAsync(int assessmentId, int studentId)
        {
            // Mock: no-op
            return Task.CompletedTask;
        }

        public Task UpdateAssessmentCompletionAsync(int assessmentId)
        {
            // Mock: no-op
            return Task.CompletedTask;
        }

        // Moderation lifecycle (mock: no-op / passthrough)
        public Task VerifyAssessmentAsync(int assessmentId, bool verified)
            => Task.CompletedTask;

        public Task PublishAssessmentAsync(int assessmentId, bool published)
            => Task.CompletedTask;

        // Fee payment reads (mock: empty)
        public Task<IReadOnlyList<AutoTable.Models.FeePaymentSummary>> GetFeePaymentsAsync(int? classId = null, int? termId = null)
            => Task.FromResult<IReadOnlyList<AutoTable.Models.FeePaymentSummary>>(Array.Empty<AutoTable.Models.FeePaymentSummary>());

        public Task<IReadOnlyList<string>> GetTermsAsync()
            => Task.FromResult<IReadOnlyList<string>>(_mock.Terms);

        public Task<IReadOnlyList<SimpleLookup>> GetTermLookupsAsync()
            => Task.FromResult<IReadOnlyList<SimpleLookup>>(_mock.Terms.Select((t, i) => new SimpleLookup { Id = i + 1, Name = t }).ToList());

        public Task<IReadOnlyList<string>> GetAcademicYearsAsync()
            => Task.FromResult<IReadOnlyList<string>>(_mock.AcademicYears);

        public Task<SimpleLookup> CreateAcademicYearAsync(string name)
            => Task.FromResult(new SimpleLookup { Id = 0, Name = name });

        public Task<IReadOnlyList<string>> GetStreamsAsync()
            => Task.FromResult<IReadOnlyList<string>>(_mock.Streams);

        public Task<IReadOnlyList<string>> GetAllStudentsAsync()
            => Task.FromResult<IReadOnlyList<string>>(_mock.AllStudents);

        public Task<Models.StudentPerformanceDetail> GetStudentPerformanceDetailAsync(string studentName, string className, string subject, string academicYear, string term, string stream)
        {
            var detail = _mock.GetStudentPerformanceDetail(studentName, className, subject, academicYear, term, stream);
            return Task.FromResult(detail);
        }

        public Task<IReadOnlyList<Student>> GetStudentsAsync()
        {
            // Map simple student list into Student model with minimal fields
            var list = _mock.AllStudents.Select((name, i) => new Student
            {
                Id = i + 1,
                FullName = name,
                LIN = string.Empty,
                AdmissionNumber = string.Empty,
                IsActive = true
            }).ToList();
            return Task.FromResult<IReadOnlyList<Student>>(list);
        }

        public Task<Student?> GetStudentByIdAsync(int id)
        {
            var name = _mock.AllStudents.ElementAtOrDefault(id - 1);
            if (name == null) return Task.FromResult<Student?>(null);
            return Task.FromResult<Student?>(new Student { Id = id, FullName = name, IsActive = true });
        }

        public Task<Student?> UpdateStudentAsync(Student student)
            => Task.FromResult<Student?>(student);

        public Task TerminateStudentAsync(int studentId, StudentTerminationReason reason, DateTime date, bool anonymize = false)
            => Task.CompletedTask;

        public Task SaveEnrollmentAsync(EnrollmentFormData enrollment)
            => Task.CompletedTask;

        public Task<IReadOnlyList<EnrollmentFormData>> GetEnrollmentsAsync()
            => Task.FromResult<IReadOnlyList<EnrollmentFormData>>(Array.Empty<EnrollmentFormData>());

        public Task<IReadOnlyList<SimpleLookup>> GetClassesAsync()
            => Task.FromResult<IReadOnlyList<SimpleLookup>>(_mock.Classes.Select((c, i) => new SimpleLookup { Id = i + 1, Name = c }).ToList());

        public Task<SimpleLookup> CreateClassAsync(string name, int? classTeacherId = null)
            => Task.FromResult(new SimpleLookup { Id = 0, Name = name });

        public Task<IReadOnlyDictionary<int, string>> GetClassTeacherNamesAsync()
            => Task.FromResult<IReadOnlyDictionary<int, string>>(new Dictionary<int, string>());

        public Task<IReadOnlyList<SimpleLookup>> GetSubjectsAsync()
            => Task.FromResult<IReadOnlyList<SimpleLookup>>(_mock.Subjects.Select((s, i) => new SimpleLookup { Id = i + 1, Name = s }).ToList());

        public Task<SimpleLookup> CreateSubjectAsync(string name)
            => Task.FromResult(new SimpleLookup { Id = 0, Name = name });

        public Task DeleteSubjectAsync(int subjectId)
            => Task.CompletedTask;

        public Task<IReadOnlyList<SimpleLookup>> GetSubjectsForClassAsync(int classId)
            => Task.FromResult<IReadOnlyList<SimpleLookup>>(Array.Empty<SimpleLookup>());

        public Task<IReadOnlyList<SimpleLookup>> GetStreamsForClassAsync(int classId)
        {
            // Mock: return all streams (no per-class mapping in mock)
            var list = _mock.Streams.Select((s, i) => new SimpleLookup { Id = i + 1, Name = s }).ToList();
            return Task.FromResult<IReadOnlyList<SimpleLookup>>(list);
        }

        public Task<SimpleLookup> CreateStreamAsync(string name, int? classId = null)
        {
            // Mock: return a SimpleLookup without persisting; ignore class assignment
            return Task.FromResult(new SimpleLookup { Id = 0, Name = name });
        }

        public Task<IReadOnlyList<SimpleLookup>> GetAllStreamsAsync()
            => Task.FromResult<IReadOnlyList<SimpleLookup>>(_mock.Streams.Select((s, i) => new SimpleLookup { Id = i + 1, Name = s }).ToList());

        public Task AssignStreamToClassAsync(int classId, int streamId)
        {
            // Mock: no-op
            return Task.CompletedTask;
        }

        public Task RemoveStreamFromClassAsync(int classId, int streamId)
        {
            // Mock: no-op
            return Task.CompletedTask;
        }

        public Task AssignStudentToStreamAsync(int studentId, int streamId)
        {
            // Mock: no-op
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TermFee>> GetTermFeesAsync()
            => Task.FromResult<IReadOnlyList<TermFee>>(Array.Empty<TermFee>());

        public Task<TermFee> SetTermFeeAsync(int termId, int classId, double amount)
            => Task.FromResult(new TermFee { Id = 0, TermId = termId, ClassId = classId, Amount = amount, TermName = string.Empty, ClassName = string.Empty });

        public Task CreateFeePaymentAsync(int studentId, double amount, int? termId = null, int? recordedByUserId = null, string? description = null)
        {
            // Mock: no-op
            return Task.CompletedTask;
        }

        public Task<SimpleLookup> CreateTermAsync(string name, DateTime? startDate = null, DateTime? endDate = null)
        {
            return Task.FromResult(new SimpleLookup { Id = 0, Name = name });
        }

        // Teacher CRUD (mock)
        public Task<IReadOnlyList<AutoTable.Models.Teacher>> GetTeachersAsync()
        {
            return Task.FromResult<IReadOnlyList<AutoTable.Models.Teacher>>(Array.Empty<AutoTable.Models.Teacher>());
        }

        public Task<AutoTable.Models.Teacher> CreateTeacherAsync(AutoTable.Models.Teacher teacher)
        {
            teacher.Id = 0;
            return Task.FromResult(teacher);
        }

        public Task<AutoTable.Models.Teacher?> UpdateTeacherAsync(AutoTable.Models.Teacher teacher)
        {
            return Task.FromResult<AutoTable.Models.Teacher?>(teacher);
        }

        public Task DeleteTeacherAsync(int teacherId)
        {
            return Task.CompletedTask;
        }

        public Task<SimpleLookup?> UpdateTermAsync(int termId, string name, DateTime? startDate = null, DateTime? endDate = null)
        {
            return Task.FromResult<SimpleLookup?>(new SimpleLookup { Id = termId, Name = name });
        }

        public Task DeleteTermAsync(int termId)
        {
            return Task.CompletedTask;
        }

        // Budget line CRUD (mock)
        public Task<IReadOnlyList<AutoTable.Models.BudgetLine>> GetBudgetLinesAsync(string? financialYear = null)
            => Task.FromResult<IReadOnlyList<AutoTable.Models.BudgetLine>>(Array.Empty<AutoTable.Models.BudgetLine>());

        public Task<AutoTable.Models.BudgetLine> CreateBudgetLineAsync(AutoTable.Models.BudgetLine line)
        {
            line.Id = 0;
            return Task.FromResult(line);
        }

        public Task<AutoTable.Models.BudgetLine?> UpdateBudgetLineAsync(AutoTable.Models.BudgetLine line)
            => Task.FromResult<AutoTable.Models.BudgetLine?>(line);

        public Task DeleteBudgetLineAsync(int budgetLineId)
            => Task.CompletedTask;
    }
}
