using AutoTable.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.Services
{
    // Lightweight adapter that exposes the in-memory MockDataService via the IDataService interface.
    // Used as a safe fallback when the real database cannot be initialized.
    public class MockDataServiceAdapter : IDataService
    {
        private readonly MockDataService _mock = MockDataService.Instance;

        public Task CreateAssessmentAsync(AssessmentItem item)
        {
            // No-op for mock; callers expect completion only.
            return Task.CompletedTask;
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

        public Task<IReadOnlyList<string>> GetTermsAsync()
            => Task.FromResult<IReadOnlyList<string>>(_mock.Terms);

        public Task<IReadOnlyList<string>> GetAcademicYearsAsync()
            => Task.FromResult<IReadOnlyList<string>>(_mock.AcademicYears);

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

        public Task<SimpleLookup> CreateClassAsync(string name)
            => Task.FromResult(new SimpleLookup { Id = 0, Name = name });

        public Task<IReadOnlyList<SimpleLookup>> GetSubjectsAsync()
            => Task.FromResult<IReadOnlyList<SimpleLookup>>(_mock.Subjects.Select((s, i) => new SimpleLookup { Id = i + 1, Name = s }).ToList());

        public Task<SimpleLookup> CreateSubjectAsync(string name)
            => Task.FromResult(new SimpleLookup { Id = 0, Name = name });

        public Task DeleteSubjectAsync(int subjectId)
            => Task.CompletedTask;

        public Task<IReadOnlyList<SimpleLookup>> GetSubjectsForClassAsync(int classId)
            => Task.FromResult<IReadOnlyList<SimpleLookup>>(Array.Empty<SimpleLookup>());
    }
}
