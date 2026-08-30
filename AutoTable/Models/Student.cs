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

        // ── Display helpers (plain strings so the model stays UI-framework agnostic) ──
        /// <summary>"Active" or "Inactive" — drives the status dot and its label.</summary>
        public string StatusText => IsActive ? "Active" : "Inactive";
        /// <summary>Cause of inactivity: "Completed course" for Completed, otherwise "Terminated".</summary>
        public string InactiveCauseText => !IsActive
            ? TerminationReason switch
            {
                StudentTerminationReason.Completed => "Completed course",
                _ => "Terminated"
            }
            : string.Empty;
        /// <summary>
        /// Drives the status dot color. Returns "Active" for active students,
        /// or the termination reason name for inactive students.
        /// </summary>
        public string StatusDotSource => IsActive ? "Active" : TerminationReason.ToString();
        /// <summary>
        /// Human-readable label for the status badge: "Active", "Completed", "Expelled",
        /// "Changed School", or "Terminated".
        /// </summary>
        public string StatusLabel => IsActive ? "Active" : TerminationReason switch
        {
            StudentTerminationReason.Completed => "Completed",
            StudentTerminationReason.Expelled => "Expelled",
            StudentTerminationReason.ChangedSchool => "Changed School",
            _ => "Terminated"
        };
        /// <summary>Year the student was terminated (null if still active).</summary>
        public int? TerminationYear => TerminationDate?.Year;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Convenience properties for UI
        public string? ClassName { get; set; }
        public string? StreamName { get; set; }
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
