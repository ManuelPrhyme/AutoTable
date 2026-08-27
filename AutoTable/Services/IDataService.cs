using AutoTable.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoTable.Services
{
    public interface IDataService
    {
        Task<IReadOnlyList<AssessmentItem>> GetAssessmentsAsync();
        Task<IReadOnlyList<Models.GradebookRow>> GetGradebookAsync(string? className, string? subject, string? academicYear = null, string? term = null, string? stream = null, string? studentName = null);
        Task<IReadOnlyList<Models.StudentMarkRow>> GetStudentMarksAsync(string className, string subject, string assessmentName);

        // Lookup lists for filters
        Task<IReadOnlyList<string>> GetTermsAsync();
        // Returns terms with Ids for UI that needs real DB ids
        Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetTermLookupsAsync();
        // Returns the currently active term (IsActive), or null when no term is active
        Task<AutoTable.Models.SimpleLookup?> GetActiveTermAsync();
        Task<IReadOnlyList<string>> GetAcademicYearsAsync();
        Task<AutoTable.Models.SimpleLookup> CreateAcademicYearAsync(string name);
        Task<IReadOnlyList<string>> GetStreamsAsync();
        Task<IReadOnlyList<string>> GetAllStudentsAsync();

        // Streams and class-stream relations
        Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetStreamsForClassAsync(int classId);
        Task<AutoTable.Models.SimpleLookup> CreateStreamAsync(string name, int? classId = null);
        // Assign a student's stream (update enrollment)
        Task AssignStudentToStreamAsync(int studentId, int streamId);
        Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetAllStreamsAsync();
        Task AssignStreamToClassAsync(int classId, int streamId, int? streamTeacherId = null);
        Task RemoveStreamFromClassAsync(int classId, int streamId);

        // Term fee management
        Task<IReadOnlyList<AutoTable.Models.TermFee>> GetTermFeesAsync();
        Task<AutoTable.Models.TermFee> SetTermFeeAsync(int termId, int classId, double amount);
        // Fee payments
        Task CreateFeePaymentAsync(int studentId, double amount, int? termId = null, int? recordedByUserId = null, string? description = null);
        // Term management
        Task<AutoTable.Models.SimpleLookup> CreateTermAsync(string name, DateTime? startDate = null, DateTime? endDate = null);
        Task<AutoTable.Models.SimpleLookup?> UpdateTermAsync(int termId, string name, DateTime? startDate = null, DateTime? endDate = null);
        Task DeleteTermAsync(int termId);
        /// <summary>Makes the specified term active and deactivates all others.</summary>
        Task SetActiveTermAsync(int termId);
        /// <summary>Deactivates the specified term (sets IsActive=false).</summary>
        Task DeactivateTermAsync(int termId);

        // Student performance detail
        Task<Models.StudentPerformanceDetail> GetStudentPerformanceDetailAsync(string studentName, string className, string subject, string academicYear, string term, string stream);

        // Report-card assembly
        Task<Models.ReportCardSheetModel?> GetReportCardSheetAsync(string studentName, string className, string term);
        /// <summary>Returns all active students in a class with overall average across all subjects for a given term.
        /// Used by the Report Cards list view.</summary>
        Task<IReadOnlyList<Models.ReportCardRow>> GetReportCardListAsync(string className, string? term, string? stream);
        /// <summary>Returns mid-term slip data for all students in a class for a given term.</summary>
        Task<IReadOnlyList<Models.MidTermSlipModel>> GetMidTermSlipsAsync(string className, string? term, string? stream);

        Task<IReadOnlyList<Student>> GetStudentsAsync();
        Task<Student?> GetStudentByIdAsync(int id);
        Task<Student> CreateStudentAsync(Student student);
        Task<Student> CreateStudentWithInitialDataAsync(Student student, double? initialFeeAmount = null, IEnumerable<(int AssessmentId, double? Mark, string? Grade)>? initialMarks = null);
        Task<Student?> UpdateStudentAsync(Student student);
        Task TerminateStudentAsync(int studentId, StudentTerminationReason reason, DateTime date, bool anonymize = false);
        Task<IReadOnlyList<TerminationLogItem>> GetTerminationLogAsync();
        Task SaveEnrollmentAsync(EnrollmentFormData enrollment);
        Task<IReadOnlyList<EnrollmentFormData>> GetEnrollmentsAsync();
        /// <summary>Returns true if the given LIN is already assigned to an active student.</summary>
        Task<bool> IsLinTakenAsync(string lin);

        // Class & Subject management
        Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetClassesAsync();
        // Class creation: any teacher (registered or student teacher) may be assigned,
        // and the class is tied to a grading system (falls back to school default when null).
        Task<AutoTable.Models.SimpleLookup> CreateClassAsync(string name, int? classTeacherId = null, int? gradingSystemId = null);
        Task UpdateClassAsync(int classId, string name, int? classTeacherId, int? gradingSystemId);
        Task<IReadOnlyDictionary<int, string>> GetClassTeacherNamesAsync();
        Task<IReadOnlyDictionary<int, string>> GetClassGradingSystemNamesAsync();
        Task DeleteClassAsync(int classId);

        // Grading systems (named scales with promotion/repeat bands)
        Task<IReadOnlyList<AutoTable.Models.GradingSystemInfo>> GetGradingSystemsAsync();
        Task<AutoTable.Models.GradingSystemInfo> CreateGradingSystemAsync(string name, bool isDefault = false, double passMark = 50);
        Task DeleteGradingSystemAsync(int gradingSystemId);
        Task<IReadOnlyList<AutoTable.Models.GradeBandInfo>> GetGradeBandsAsync(int gradingSystemId);
        Task CreateGradeBandAsync(int gradingSystemId, string label, double minScore, double maxScore,
            bool isPromotionalPass, bool isRepeater, bool isPromotionalFail);
        /// <summary>Resolve the grading system (with bands + passmark) for a class.
        /// Falls back to the school default when the class has no explicit system.</summary>
        Task<AutoTable.Models.GradingSystemInfo?> GetClassGradingSystemAsync(int classId);

        // Promotion / repeat (Term 3 move-up)
        Task<IReadOnlyList<AutoTable.Models.PromotionRow>> GetPromotionOverviewAsync(int? classId = null);
        Task<AutoTable.Models.SimpleLookup?> SuggestNextClassAsync(int currentClassId);
        Task PromoteStudentAsync(int studentId, int? targetClassId = null);
        Task RepeatStudentAsync(int studentId);
        Task ShiftStudentClassAsync(int studentId, int targetClassId);
        Task ResetPromotionAsync(int studentId);
        /// <summary>Batch-process all pending students: promote those who pass, repeat those who fail.</summary>
        Task<int> ProcessAllPromotionsAsync(int? classId = null);

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

        // Moderation lifecycle (Phase 4)
        Task VerifyAssessmentAsync(int assessmentId, bool verified);
        Task PublishAssessmentAsync(int assessmentId, bool published);

        // Fee payment reads (Phase 5)
        Task<IReadOnlyList<AutoTable.Models.FeePaymentSummary>> GetFeePaymentsAsync(int? classId = null, int? termId = null);

        // Defaulters / cohort finance analytics (P5.5)
        Task<IReadOnlyList<AutoTable.Models.DefaulterRecord>> GetDefaultersAsync(int? termId = null, int? classId = null, decimal? minBalance = null);
        Task<IReadOnlyList<AutoTable.Models.CohortSummary>> GetCohortSummariesAsync(int? termId = null);

        // Student credits (overpayment carry-forward)
        Task<IReadOnlyList<AutoTable.Models.StudentCredit>> GetStudentCreditsAsync(int studentId);
        Task<double> GetAvailableCreditAsync(int studentId, int termId);

        // Teacher CRUD (maps to UserEntity with Role="Teacher")
        Task<IReadOnlyList<AutoTable.Models.Teacher>> GetTeachersAsync();
        Task<AutoTable.Models.Teacher> CreateTeacherAsync(AutoTable.Models.Teacher teacher);
        Task<AutoTable.Models.Teacher?> UpdateTeacherAsync(AutoTable.Models.Teacher teacher);
        Task DeleteTeacherAsync(int teacherId);

        // Budget line CRUD
        Task<IReadOnlyList<AutoTable.Models.BudgetLine>> GetBudgetLinesAsync(string? financialYear = null);
        Task<AutoTable.Models.BudgetLine> CreateBudgetLineAsync(AutoTable.Models.BudgetLine line);
        Task<AutoTable.Models.BudgetLine?> UpdateBudgetLineAsync(AutoTable.Models.BudgetLine line);
        Task DeleteBudgetLineAsync(int budgetLineId);

        // School settings (singleton)
        Task<AutoTable.Models.SchoolSettings> GetSchoolSettingsAsync();
        Task<AutoTable.Models.SchoolSettings> UpdateSchoolSettingsAsync(AutoTable.Models.SchoolSettings settings);

        // Head teacher comments per student per term
        Task<string> GetHeadTeacherCommentAsync(int studentId, int? termId);
        Task SaveHeadTeacherCommentAsync(int studentId, int? termId, string comment, string headTeacherName);
    }
}
