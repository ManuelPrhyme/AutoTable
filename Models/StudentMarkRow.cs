namespace AutoTable.Models
{
    public class StudentMarkRow
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public double? Mark { get; set; }
        public string Grade { get; set; } = "-";
        public string Remarks { get; set; } = string.Empty;
        public bool IsEditable { get; set; } = true;

        public string MarkDisplay => Mark.HasValue ? Mark.Value.ToString("0.#") : string.Empty;
    }
}
