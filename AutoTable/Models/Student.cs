using System;

namespace AutoTable.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string LIN { get; set; } = string.Empty; // user supplied unique id
        public string FullName { get; set; } = string.Empty;
        public string? AdmissionNumber { get; set; }
        public int? ClassId { get; set; }
        public int? StreamId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public bool IsActive { get; set; } = true;
        public StudentTerminationReason TerminationReason { get; set; } = StudentTerminationReason.None;
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
    }
}
