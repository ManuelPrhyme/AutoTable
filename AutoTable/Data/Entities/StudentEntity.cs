using System;
using System.Collections.Generic;

namespace AutoTable.Data.Entities
{
    public class StudentEntity
    {
        public int Id { get; set; }
        public string LIN { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AdmissionNumber { get; set; }
        public int? ClassId { get; set; }
        public ClassEntity? Class { get; set; }
        public int? StreamId { get; set; }
        public StreamEntity? Stream { get; set; }
        // End-of-year promotion (Term 3 move-up): 0 = Pending, 1 = Promoted,
        // 2 = Repeat, 3 = Shifted (manual class change)
        public int PromotionStatus { get; set; }
        public int? PromotedToClassId { get; set; }
        public ClassEntity? PromotedToClass { get; set; }
        public DateTime? PromotionProcessedAt { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public bool IsActive { get; set; } = true;
        public int TerminationReason { get; set; } // store enum as int
        public DateTime? TerminationDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Extended enrollment fields
        public string? GuardianName { get; set; }
        public string? GuardianRelationship { get; set; }
        public string? GuardianPhone { get; set; }
        public string? GuardianEmail { get; set; }
        public string? GuardianAddress { get; set; }
        public bool HasCustodyDocuments { get; set; }
        public string? ResidenceProofType { get; set; }
        public string? ResidenceDistrict { get; set; }
        public string? ResidenceZone { get; set; }
        public bool HasImmunizationCard { get; set; }
        public bool HasMedicalExamReport { get; set; }
        public string? AllergiesOrConditions { get; set; }
        public string? HealthInsurance { get; set; }
        public string? EmergencyName { get; set; }
        public string? EmergencyRelationship { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? AuthorizedPickupPerson { get; set; }

        public ICollection<MarkEntity> Marks { get; set; } = new List<MarkEntity>();
        public ICollection<FeePaymentEntity> FeePayments { get; set; } = new List<FeePaymentEntity>();
    }

        public class ClassEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        // Optional class teacher — must be an existing registered teacher (Users.Role == "Teacher")
        public int? ClassTeacherId { get; set; }
        public UserEntity? ClassTeacher { get; set; }
        // Optional grading system — null falls back to the school default
        public int? GradingSystemId { get; set; }
        public GradingSystemEntity? GradingSystem { get; set; }
        public ICollection<StudentEntity> Students { get; set; } = new List<StudentEntity>();
        public ICollection<AssessmentEntity> Assessments { get; set; } = new List<AssessmentEntity>();
        public ICollection<ClassSubjectEntity> ClassSubjects { get; set; } = new List<ClassSubjectEntity>();
        public ICollection<ClassStreamEntity> ClassStreams { get; set; } = new List<ClassStreamEntity>();
    }

    // A named grading system (e.g. "CBSE-10", "IGCSE") owned by the school
    public class GradingSystemEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        // The mark (%) a student must average to be promoted under this system.
        // The class author sets this when creating the grading scale.
        public double PassMark { get; set; } = 50;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<GradeBandEntity> GradeBands { get; set; } = new List<GradeBandEntity>();
                public ICollection<ClassEntity> Classes { get; set; } = new List<ClassEntity>();
    }

    // A single grade band/range within a grading system (e.g. A:90-100, B:75-89 ...)
    public class GradeBandEntity
    {
        public int Id { get; set; }
        public int GradingSystemId { get; set; }
        public GradingSystemEntity? GradingSystem { get; set; }
        public string Label { get; set; } = string.Empty;          // e.g. "A", "B+", "C4"
        public double MinScore { get; set; }
        public double MaxScore { get; set; }
        public bool IsPromotionalPass { get; set; }                 // this band passes for promotion
        public bool IsRepeater { get; set; }                        // this band means "repeat the class"
        public bool IsPromotionalFail { get; set; }                 // this band fails promotion
    }

    public class StreamEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<StudentEntity> Students { get; set; } = new List<StudentEntity>();
        public ICollection<ClassStreamEntity> ClassStreams { get; set; } = new List<ClassStreamEntity>();
    }

    public class SubjectEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<AssessmentEntity> Assessments { get; set; } = new List<AssessmentEntity>();
        public ICollection<ClassSubjectEntity> ClassSubjects { get; set; } = new List<ClassSubjectEntity>();
        public ICollection<AssessmentSubjectEntity> AssessmentSubjects { get; set; } = new List<AssessmentSubjectEntity>();
        public ICollection<MarkEntity> Marks { get; set; } = new List<MarkEntity>();
    }

    public class ClassSubjectEntity
    {
        public int ClassId { get; set; }
        public ClassEntity? Class { get; set; }

        public int SubjectId { get; set; }
        public SubjectEntity? Subject { get; set; }
    }

    /// <summary>
    /// Links a multi-subject assessment to each subject it covers. A single-subject
    /// assessment instead uses AssessmentEntity.SubjectId directly and has no links.
    /// </summary>
    public class AssessmentSubjectEntity
    {
        public int AssessmentId { get; set; }
        public AssessmentEntity? Assessment { get; set; }
        public int SubjectId { get; set; }
        public SubjectEntity? Subject { get; set; }
    }

    public class TermEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        // Mark which term is currently active
        public bool IsActive { get; set; }
    }

    public class AcademicYearEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AssessmentEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public ClassEntity? Class { get; set; }
        // For a single-subject assessment this is the subject's id. For a multi-subject
        // assessment (AllInClass / SpecificSubjects / AllInSchool) it is null and the
        // covered subjects live in the AssessmentSubjects link table.
        public int? SubjectId { get; set; }
        public SubjectEntity? Subject { get; set; }
        public int AcademicYearId { get; set; }
        public AcademicYearEntity? AcademicYear { get; set; }
        public int TermId { get; set; }
        public TermEntity? Term { get; set; }
        // Optional: when assessment targets a single stream only
        public int? StreamId { get; set; }
        public StreamEntity? Stream { get; set; }
        /// <summary>Comma-separated stream ids an assessment targets (multi-stream support). When set, marks rosters and report cards respect every listed stream.</summary>
        public string? StreamIdsCsv { get; set; }
        // If true the assessment applies to the whole class; if false and StreamId set it applies only to that stream
        public bool IsClassWide { get; set; } = true;
        // True for school-wide (AllInSchool) assessments: applies to every class, and the
        // ClassId merely anchors the record (marks entry / report cards match any class).
        public bool IsSchoolWide { get; set; }
        public int WeightPercent { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsVerified { get; set; }
        public bool IsPublished { get; set; }
        public int MarksEnteredPercent { get; set; }
        /// <summary>When the assessment record was first created (used for creation-order sorting/display).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        /// <summary>AssessmentPromotionRole values: 0=None (Just an assessment), 1=CountsTowardPromotion (Contributory End-of-Year), 2=PromotionExam (End-of-Year), 3=EndOfTerm, 4=ContributoryEndOfTerm — maps to AssessmentPromotionRole enum.</summary>
        public int PromotionRole { get; set; }
        /// <summary>Optional: the teacher who authored/created this assessment.</summary>
        public int? AuthorUserId { get; set; }
        public UserEntity? AuthorUser { get; set; }
        /// <summary>Display name of the author (stored for when the user record may not exist in DB).</summary>
        public string? AuthorName { get; set; }

        public ICollection<MarkEntity> Marks { get; set; } = new List<MarkEntity>();
        public ICollection<AssessmentSubjectEntity> AssessmentSubjects { get; set; } = new List<AssessmentSubjectEntity>();
    }

    public class MarkEntity
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public StudentEntity? Student { get; set; }
        public int AssessmentId { get; set; }
        public AssessmentEntity? Assessment { get; set; }
        // The subject this mark is for. Set for marks in multi-subject assessments;
        // left null for single-subject assessments (resolved via Assessment.SubjectId).
        public int? SubjectId { get; set; }
        public SubjectEntity? Subject { get; set; }
        public double? Mark { get; set; }
        public string? Grade { get; set; }
        public string? Remarks { get; set; }
        public int? EnteredByUserId { get; set; }
        public DateTime? EnteredAt { get; set; }
    }

    public class FeePaymentEntity
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public StudentEntity? Student { get; set; }
        public double Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        // Associate a payment with a particular term (optional)
        public int? TermId { get; set; }
        public TermEntity? Term { get; set; }
        public int? RecordedByUserId { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Tracks overpayment credits carried forward from one term to the next.
    /// When a student pays more than owed in a term, the excess is stored here
    /// and automatically applied to the next term's balance.
    /// </summary>
    public class StudentCreditEntity
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public StudentEntity? Student { get; set; }
        /// <summary>The term where the overpayment occurred (source of the credit).</summary>
        public int FromTermId { get; set; }
        public TermEntity? FromTerm { get; set; }
        /// <summary>The term this credit was applied to (null = unapplied).</summary>
        public int? AppliedToTermId { get; set; }
        public TermEntity? AppliedToTerm { get; set; }
        public double Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        /// <summary>When null, the credit is available; set when applied.</summary>
        public DateTime? AppliedAt { get; set; }
        public string? Description { get; set; }
    }

    public class UserEntity
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Extended teacher profile fields
        public string? Phone { get; set; }
        public string? SubjectsTaught { get; set; }       // comma-separated
        public string? ClassesTaught { get; set; }        // comma-separated
        public string? NextOfKinName { get; set; }
        public string? NextOfKinRelationship { get; set; }
        public string? NextOfKinPhone { get; set; }
        public string? PreviousSchools { get; set; }      // comma-separated
        public bool IsRegisteredTeacher { get; set; }     // registered with the teachers' board
        public bool IsStudentTeacher { get; set; }        // still a student teacher
    }

    public class TerminationLogEntity
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public StudentEntity? Student { get; set; }
        public string StudentNameAtTermination { get; set; } = string.Empty;
        public int TerminationReason { get; set; }
        public DateTime TerminationDate { get; set; }
        public bool Anonymized { get; set; }
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    }

    public class EnrollmentEntity
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string LIN { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Religion { get; set; } = string.Empty;
        public string PreviousSchool { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianRelationship { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        public string GuardianEmail { get; set; } = string.Empty;
        public string GuardianAddress { get; set; } = string.Empty;
        public bool HasCustodyDocuments { get; set; }
        public string ResidenceProofType { get; set; } = string.Empty;
        public string ResidenceDistrict { get; set; } = string.Empty;
        public string ResidenceZone { get; set; } = string.Empty;
        public bool HasImmunizationCard { get; set; }
        public bool HasMedicalExamReport { get; set; }
        public string AllergiesOrConditions { get; set; } = string.Empty;
        public string HealthInsurance { get; set; } = string.Empty;
        public string EmergencyName { get; set; } = string.Empty;
        public string EmergencyRelationship { get; set; } = string.Empty;
        public string EmergencyPhone { get; set; } = string.Empty;
        public string AuthorizedPickupPerson { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "New";
    }

    // Join entity to associate Classes with Streams (many-to-many)
    public class ClassStreamEntity
    {
        public int ClassId { get; set; }
        public ClassEntity? Class { get; set; }

        public int StreamId { get; set; }
        public StreamEntity? Stream { get; set; }

        // Optional stream-level teacher — falls back to the class teacher when null.
        public int? StreamTeacherId { get; set; }
        public UserEntity? StreamTeacher { get; set; }
    }

    // Term fee entity: amount to be charged for a given Class during a Term
    public class TermFeeEntity
    {
        public int Id { get; set; }
        public int TermId { get; set; }
        public TermEntity? Term { get; set; }

        public int ClassId { get; set; }
        public ClassEntity? Class { get; set; }

        public double Amount { get; set; }
    }

    // Budget line entity: a single budget category with budgeted and spent amounts
    public class BudgetLineEntity
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Budgeted { get; set; }
        public decimal Spent { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Global school configuration — name, head teacher, address, logo. Singleton row (Id=1).</summary>
    public class SchoolSettingsEntity
    {
        public int Id { get; set; } = 1;
        public string SchoolName { get; set; } = "AutoTable Academy";
        public string SchoolAddress { get; set; } = string.Empty;
        public string SchoolPhone { get; set; } = string.Empty;
        public string HeadTeacherName { get; set; } = string.Empty;
        public string Motto { get; set; } = string.Empty;
        /// <summary>Raw image bytes for the school logo (stored as BLOB in SQLite).</summary>
        public byte[]? LogoBytes { get; set; }
    }

    /// <summary>Per-student head teacher comment on the report card.</summary>
    public class HeadTeacherCommentEntity
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public StudentEntity? Student { get; set; }
        public int? TermId { get; set; }
        public TermEntity? Term { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string HeadTeacherName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
