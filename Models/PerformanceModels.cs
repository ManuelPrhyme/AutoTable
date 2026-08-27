namespace AutoTable.Models
{
    public class ClassBreakdown
    {
        public string ClassName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public double Average { get; set; }
        public int AtRisk { get; set; }
        public int Excellent { get; set; }
        public string Trend { get; set; } = string.Empty;
    }

    public class ModerationItem
    {
        // Numeric DB id of the assessment (0 when unknown) — used by verify/publish actions
        public int AssessmentId { get; set; }
        public string AssessmentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int EntryCount { get; set; }
        public string SubmittedBy { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
    }

    public class ReportCardRow
    {
        public int Rank { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public double Average { get; set; }
        public string Status { get; set; } = string.Empty;
        /// <summary>Fee payment status for finance-filtered printing: "Paid", "Partial", "Unpaid", or "N/A".</summary>
        public string FeeStatus { get; set; } = "N/A";
        public double ExpectedAmount { get; set; }
        public double PaidAmount { get; set; }
        public double Balance => ExpectedAmount - PaidAmount;
    }

    public class AutomationItem
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>A single subject result on a mid-term slip.</summary>
    public class MidTermSubjectResult
    {
        public string Subject { get; set; } = string.Empty;
        public string AssessmentName { get; set; } = string.Empty;
        public double Mark { get; set; }
        public string Grade { get; set; } = "-";
        public string Remarks { get; set; } = string.Empty;
    }

    /// <summary>Mid-term slip model for one student — compact A4 layout (3-4 slips per page).</summary>
    public class MidTermSlipModel
    {
        public string SchoolName { get; set; } = "AutoTable Academy";
        public string Term { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Stream { get; set; } = string.Empty;
        public string ClassTeacher { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        public List<MidTermSubjectResult> Results { get; set; } = new();
        public double OverallAverage { get; set; }
        public string OverallGrade { get; set; } = "-";
        public int Rank { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ClassTeacherLine => string.IsNullOrEmpty(ClassTeacher)
            ? string.Empty
            : $"Class Teacher: {ClassTeacher}";
    }
}
