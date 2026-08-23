using System;

namespace AutoTable.Models
{
    /// <summary>
    /// Determines which subjects an assessment applies to.
    /// </summary>
    public enum AssessmentScope
    {
        /// <summary>Assessment applies to a single subject (default).</summary>
        Single,
        /// <summary>Assessment applies to all subjects in the selected class/stream.</summary>
        AllInClass,
        /// <summary>Assessment applies to a user-selected set of subjects in the class.</summary>
        SpecificSubjects,
        /// <summary>Assessment applies to all subjects across all classes (general/school-wide exam).</summary>
        AllInSchool
    }

    public class AssessmentItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        /// <summary>Which subjects this assessment covers. Used during creation; each subject gets its own AssessmentEntity.</summary>
        public AssessmentScope Scope { get; set; } = AssessmentScope.Single;
        // Optional: target a specific stream (null means whole class unless IsClassWide=false and StreamId set)
        public int? StreamId { get; set; }
        public string? StreamName { get; set; }
        public bool IsClassWide { get; set; } = true;
        public int WeightPercent { get; set; }
        public DateTime DueDate { get; set; }
        public int MarksEnteredPercent { get; set; }
        public bool IsVerified { get; set; }
        public bool IsPublished { get; set; }
        public string StatusLabel => IsPublished ? "Published" : IsVerified ? "Verified" : MarksEnteredPercent >= 100 ? "Complete" : "In Progress";
    }
}
