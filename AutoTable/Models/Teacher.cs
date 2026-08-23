namespace AutoTable.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Role { get; set; } = "Teacher";

        // Contact
        public string? Phone { get; set; }

        // Teaching assignments (comma-separated lists)
        public string? SubjectsTaught { get; set; }
        public string? ClassesTaught { get; set; }

        // Next of kin
        public string? NextOfKinName { get; set; }
        public string? NextOfKinRelationship { get; set; }
        public string? NextOfKinPhone { get; set; }

        // Career history (comma-separated)
        public string? PreviousSchools { get; set; }

        // Qualification status
        public bool IsRegisteredTeacher { get; set; }  // registered with the teachers' board
        public bool IsStudentTeacher { get; set; }     // still a student teacher

        /// <summary>Friendly qualification label for the UI.</summary>
        public string QualificationLabel =>
            IsRegisteredTeacher && !IsStudentTeacher ? "Registered Teacher"
            : IsStudentTeacher ? "Student Teacher"
            : "Unregistered";

        /// <summary>Next of kin display: "Name (Relationship)" or just the name if no relationship.</summary>
        public string NextOfKinDisplay =>
            string.IsNullOrWhiteSpace(NextOfKinRelationship)
                ? NextOfKinName ?? string.Empty
                : $"{NextOfKinName} ({NextOfKinRelationship})";
    }
}