using System;
using System.Collections.Generic;

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
        AllInSchool,
        /// <summary>Assessment targets a single stream of a class (covers that stream's class subjects; marks are only entered for students in that stream).</summary>
        Stream
    }

    /// <summary>
    /// Determines how an assessment counts toward the end-of-term result and the
    /// end-of-year promotion decision. An assessment is one of five states.
    /// <para>Integer values are persisted in the DB; 0-2 are legacy values and must not change.</para>
    /// </summary>
    public enum AssessmentPromotionRole
    {
        /// <summary>Just an assessment — does not count toward end-of-term or end-of-year (default).</summary>
        None = 0,
        /// <summary>Contributory (End of Year / Promotional) — contributes to the end-of-year promotion average.</summary>
        CountsTowardPromotion = 1,
        /// <summary>End of Year (Promotional) — the year-end paper that decides promotion.</summary>
        PromotionExam = 2,
        /// <summary>End of Term — the term-end paper that decides the end-of-term result.</summary>
        EndOfTerm = 3,
        /// <summary>Contributory (End of Term) — contributes to the end-of-term result.</summary>
        ContributoryEndOfTerm = 4
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
        /// <summary>When the assessment targets streams, the full set of stream ids the paper applies to (multi-stream).</summary>
        public List<int> StreamIds { get; set; } = new();
        /// <summary>Stream names corresponding to <see cref="StreamIds"/> (display + filtering).</summary>
        public List<string> StreamNames { get; set; } = new();
        public bool IsClassWide { get; set; } = true;
        public int WeightPercent { get; set; }
        public DateTime DueDate { get; set; }
        public int MarksEnteredPercent { get; set; }
        public bool IsVerified { get; set; }
        public bool IsPublished { get; set; }
        /// <summary>How this assessment contributes to promotion: None, Contributory, or Promotion Exam.</summary>
        public AssessmentPromotionRole PromotionRole { get; set; } = AssessmentPromotionRole.None;
        /// <summary>The teacher who authored this assessment (display name).</summary>
        public string? AuthorName { get; set; }
        /// <summary>The ID of the teacher who authored this assessment.</summary>
        public int? AuthorId { get; set; }
        /// <summary>Subject names covered by a multi-subject assessment (AllInClass / SpecificSubjects / AllInSchool). Empty for single-subject.</summary>
        public List<string> SubjectNames { get; set; } = new();
        /// <summary>True for school-wide (AllInSchool) assessments that apply to every class.</summary>
        public bool IsSchoolWide { get; set; }
        /// <summary>The term this assessment belongs to (for display and filtering).</summary>
        public string TermName { get; set; } = string.Empty;
        /// <summary>True when this is a multi-subject assessment (single shared paper covering several subjects).</summary>
        public bool IsMultiSubject => Scope != AssessmentScope.Single || SubjectNames.Count > 0;
        public string StatusLabel => IsPublished ? "Published" : IsVerified ? "Verified" : MarksEnteredPercent >= 100 ? "Complete" : "In Progress";
    }
}
