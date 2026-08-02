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
    }

    public class AutomationItem
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
