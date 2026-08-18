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
    }
}
