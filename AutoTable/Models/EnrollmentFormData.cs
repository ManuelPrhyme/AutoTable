using System;

namespace AutoTable.Models
{
    public class EnrollmentFormData
    {
        // Student Information
        public string FullName { get; set; } = string.Empty;
        public string LIN { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; } = DateTime.Today;
        public string Gender { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Religion { get; set; } = string.Empty;
        public string PreviousSchool { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;

        // Parent/Guardian
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianRelationship { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        public string GuardianEmail { get; set; } = string.Empty;
        public string GuardianAddress { get; set; } = string.Empty;
        public bool HasCustodyDocuments { get; set; }

        // Residency Verification
        public string ResidenceProofType { get; set; } = string.Empty;
        public string ResidenceDistrict { get; set; } = string.Empty;
        public string ResidenceZone { get; set; } = string.Empty;

        // Health Records
        public bool HasImmunizationCard { get; set; }
        public bool HasMedicalExamReport { get; set; }
        public string AllergiesOrConditions { get; set; } = string.Empty;
        public string HealthInsurance { get; set; } = string.Empty;

        // Emergency Contacts
        public string EmergencyName { get; set; } = string.Empty;
        public string EmergencyRelationship { get; set; } = string.Empty;
        public string EmergencyPhone { get; set; } = string.Empty;
        public string AuthorizedPickupPerson { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "New";
    }
}