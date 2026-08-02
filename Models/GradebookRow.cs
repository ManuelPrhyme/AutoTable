namespace AutoTable.Models
{
    public class GradebookRow
    {
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public double Cat1 { get; set; }
        public double Cat2 { get; set; }
        public double MidTerm { get; set; }
        public double EndTerm { get; set; }
        public double Average { get; set; }
        public int Rank { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
