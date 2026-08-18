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

        // Student performance detail
        Task<Models.StudentPerformanceDetail> GetStudentPerformanceDetailAsync(string studentName, string className, string subject, string academicYear, string term, string stream);

        Task<IReadOnlyList<Student>> GetStudentsAsync();
        Task<Student?> GetStudentByIdAsync(int id);
        Task<Student> CreateStudentAsync(Student student);
        Task<Student> CreateStudentWithInitialDataAsync(Student student, double? initialFeeAmount = null, IEnumerable<(int AssessmentId, double? Mark, string? Grade)>? initialMarks = null);
        Task<Student?> UpdateStudentAsync(Student student);
        Task TerminateStudentAsync(int studentId, StudentTerminationReason reason, DateTime date, bool anonymize = false);

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
    }
}
