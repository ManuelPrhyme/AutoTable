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
        public ICollection<StudentEntity> Students { get; set; } = new List<StudentEntity>();
        public ICollection<AssessmentEntity> Assessments { get; set; } = new List<AssessmentEntity>();
        public ICollection<ClassSubjectEntity> ClassSubjects { get; set; } = new List<ClassSubjectEntity>();
        public ICollection<ClassStreamEntity> ClassStreams { get; set; } = new List<ClassStreamEntity>();
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
    }

    public class ClassSubjectEntity
    {
        public int ClassId { get; set; }
        public ClassEntity? Class { get; set; }

        public int SubjectId { get; set; }
        public SubjectEntity? Subject { get; set; }
    }

    public class TermEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
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
        public int SubjectId { get; set; }
        public SubjectEntity? Subject { get; set; }
        public int AcademicYearId { get; set; }
        public AcademicYearEntity? AcademicYear { get; set; }
        public int TermId { get; set; }
        public TermEntity? Term { get; set; }
        public int WeightPercent { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsVerified { get; set; }
        public bool IsPublished { get; set; }
        public int MarksEnteredPercent { get; set; }

        public ICollection<MarkEntity> Marks { get; set; } = new List<MarkEntity>();
    }

    public class MarkEntity
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public StudentEntity? Student { get; set; }
        public int AssessmentId { get; set; }
        public AssessmentEntity? Assessment { get; set; }
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
        public int? RecordedByUserId { get; set; }
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
}
