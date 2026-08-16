using System.Collections.ObjectModel;

namespace AutoTable.Models
{
    public class StudentSubjectPerformance
    {
        public string Subject { get; set; } = string.Empty;
        public double Cat1 { get; set; }
        public double Cat2 { get; set; }
        public double MidTerm { get; set; }
        public double EndTerm { get; set; }
        public double Average { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class StudentPerformanceDetail
    {
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Stream { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public ObservableCollection<StudentSubjectPerformance> SubjectPerformances { get; set; } = new();
        public double OverallAverage { get; set; }
        public string OverallGrade { get; set; } = string.Empty;
        public int Rank { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
