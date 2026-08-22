using AutoTable.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoTable.Services
{
    public interface IDataService
    {
        Task<IReadOnlyList<AssessmentItem>> GetAssessmentsAsync();
        Task<IReadOnlyList<Models.GradebookRow>> GetGradebookAsync(string className, string subject, string? academicYear = null, string? term = null, string? stream = null, string? studentName = null);
        Task<IReadOnlyList<Models.StudentMarkRow>> GetStudentMarksAsync(string className, string subject, string assessmentName);

        // Lookup lists for filters
        Task<IReadOnlyList<string>> GetTermsAsync();
        Task<IReadOnlyList<string>> GetAcademicYearsAsync();
        Task<IReadOnlyList<string>> GetStreamsAsync();
        Task<IReadOnlyList<string>> GetAllStudentsAsync();

        // Streams and class-stream relations
        Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetStreamsForClassAsync(int classId);
        Task<AutoTable.Models.SimpleLookup> CreateStreamAsync(string name, int? classId = null);
        // Assign a student's stream (update enrollment)
        Task AssignStudentToStreamAsync(int studentId, int streamId);
        Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetAllStreamsAsync();
        Task AssignStreamToClassAsync(int classId, int streamId);
        Task RemoveStreamFromClassAsync(int classId, int streamId);

        // Term fee management
        Task<IReadOnlyList<AutoTable.Models.TermFee>> GetTermFeesAsync();
        Task<AutoTable.Models.TermFee> SetTermFeeAsync(int termId, int classId, double amount);
        // Fee payments
        Task CreateFeePaymentAsync(int studentId, double amount, int? recordedByUserId = null, string? description = null);
        // Term management
        Task<AutoTable.Models.SimpleLookup> CreateTermAsync(string name, DateTime? startDate = null, DateTime? endDate = null);
        Task<AutoTable.Models.SimpleLookup?> UpdateTermAsync(int termId, string name, DateTime? startDate = null, DateTime? endDate = null);
        Task DeleteTermAsync(int termId);

        // Student performance detail
        Task<Models.StudentPerformanceDetail> GetStudentPerformanceDetailAsync(string studentName, string className, string subject, string academicYear, string term, string stream);

        Task<IReadOnlyList<Student>> GetStudentsAsync();
        Task<Student?> GetStudentByIdAsync(int id);
        Task<Student> CreateStudentAsync(Student student);
        Task<Student> CreateStudentWithInitialDataAsync(Student student, double? initialFeeAmount = null, IEnumerable<(int AssessmentId, double? Mark, string? Grade)>? initialMarks = null);
        Task<Student?> UpdateStudentAsync(Student student);
        Task TerminateStudentAsync(int studentId, StudentTerminationReason reason, DateTime date, bool anonymize = false);
        Task<IReadOnlyList<TerminationLogItem>> GetTerminationLogAsync();
        Task SaveEnrollmentAsync(EnrollmentFormData enrollment);
        Task<IReadOnlyList<EnrollmentFormData>> GetEnrollmentsAsync();

        // Class & Subject management
        Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetClassesAsync();
        Task<AutoTable.Models.SimpleLookup> CreateClassAsync(string name);
        Task DeleteClassAsync(int classId);

        Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetSubjectsAsync();
        Task<AutoTable.Models.SimpleLookup> CreateSubjectAsync(string name);
        Task DeleteSubjectAsync(int subjectId);

        Task AssignSubjectToClassAsync(int classId, int subjectId);
        Task RemoveSubjectFromClassAsync(int classId, int subjectId);
        Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetSubjectsForClassAsync(int classId);
        Task<AutoTable.Models.AssessmentItem> CreateAssessmentAsync(AutoTable.Models.AssessmentItem item);

        // Marks / assessment CRUD (Phase 2)
        Task<AutoTable.Models.AssessmentItem?> GetAssessmentAsync(string name, string className, string subject);
        Task UpdateMarkAsync(int assessmentId, int studentId, double? mark, string? grade, string? remarks = null);
        Task DeleteMarkAsync(int assessmentId, int studentId);
        Task UpdateAssessmentCompletionAsync(int assessmentId);
    }
}
