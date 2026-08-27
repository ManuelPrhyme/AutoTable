using System.Collections.Generic;

namespace AutoTable.Models
{
    /// <summary>
    /// One row on the printed A4 report-card sheet. Rows are grouped into
    /// "promotional" (end-of-term / promotional exam) and "contributory"
    /// assessments so the report clearly separates what decides promotion
    /// from what feeds into it.
    /// </summary>
    public class ReportCardAssessmentRow
    {
        public string Subject { get; set; } = string.Empty;
        public string AssessmentName { get; set; } = string.Empty;
        public double Mark { get; set; }
        public string Grade { get; set; } = "-";
        public int WeightPercent { get; set; }
        public bool IsPromotional { get; set; }
        public bool IsPass { get; set; } = true;
        /// <summary>"PASS" / "REPEAT" / "-" — a printable verdict for the promotional row.</summary>
        public string Verdict => IsPromotional ? (IsPass ? "PASS" : "REPEAT") : "-";
    }

    /// <summary>
    /// Everything needed to render one A4 report card: the student's bio data,
    /// the promotional/contributory assessment results, overall summary and the
    /// class teacher's comment + signature block.
    /// </summary>
    public class ReportCardSheetModel
    {
        // Header / bio
        public string SchoolName { get; set; } = "AutoTable Academy";
        public string SchoolAddress { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;   // LIN
        public string ClassName { get; set; } = string.Empty;
        public string Stream { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        /// <summary>Header line under the school name, e.g. "Term 3, 2026 • Academic Year 2026".</summary>
        public string TermLabel => string.IsNullOrEmpty(AcademicYear)
            ? Term
            : $"{Term}  •  {AcademicYear}";
        public string Gender { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        public string ClassTeacher { get; set; } = string.Empty;
        /// <summary>"Class Teacher: Name" line shown in the comment block (blank when unknown).</summary>
        public string ClassTeacherLine => string.IsNullOrEmpty(ClassTeacher)
            ? string.Empty
            : $"Class Teacher: {ClassTeacher}";

        // Grading system info
        public string GradingSystemName { get; set; } = string.Empty;
        public double PassMark { get; set; } = 50;
        /// <summary>Header line shown below school name, e.g. "Grading: Uganda PLE  •  Pass mark: 50%".</summary>
        public string GradingInfoLabel => string.IsNullOrEmpty(GradingSystemName)
            ? $"Pass mark: {PassMark:0}%"
            : $"Grading: {GradingSystemName}  \u2022  Pass mark: {PassMark:0}%";

        // Results
        public List<ReportCardAssessmentRow> PromotionalAssessments { get; set; } = new();
        public List<ReportCardAssessmentRow> ContributoryAssessments { get; set; } = new();

        // Summary
        public double OverallAverage { get; set; }
        public string OverallGrade { get; set; } = "-";
        public int Rank { get; set; }
        public string Status { get; set; } = string.Empty;

        /// <summary>Populated comment block (subject strengths + class teacher remark).</summary>
        public string TeacherComment { get; set; } = string.Empty;

        // Head teacher comment
        /// <summary>Head teacher's name (shown in the signature block).</summary>
        public string HeadTeacher { get; set; } = string.Empty;
        /// <summary>"Head Teacher: Name" line shown below the head teacher comment (blank when unknown).</summary>
        public string HeadTeacherLine => string.IsNullOrEmpty(HeadTeacher)
            ? string.Empty
            : $"Head Teacher: {HeadTeacher}";
        /// <summary>Head teacher's comment on the student's performance.</summary>
        public string HeadTeacherComment { get; set; } = string.Empty;

        public bool HasPromotional => PromotionalAssessments.Count > 0;
        public bool HasContributory => ContributoryAssessments.Count > 0;

        /// <summary>Logo image bytes — set from SchoolSettings.</summary>
        public byte[]? LogoBytes { get; set; }
    }
}
