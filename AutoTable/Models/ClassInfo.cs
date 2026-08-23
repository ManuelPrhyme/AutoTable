namespace AutoTable.Models
{
    public class ClassInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string StreamsCsv { get; set; } = string.Empty;
        public string SubjectsCsv { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public string ClassTeacherName { get; set; } = string.Empty;
    }
}