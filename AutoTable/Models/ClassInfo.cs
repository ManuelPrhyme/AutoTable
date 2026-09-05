namespace AutoTable.Models
{
    using System.Collections.Generic;

    public class ClassInfo
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Streams { get; set; } = new();
        public List<string> Subjects { get; set; } = new();
        public int StudentCount { get; set; }
        public string ClassTeacherName { get; set; } = string.Empty;
        public int? ClassTeacherId { get; set; }
        public string GradingSystemName { get; set; } = string.Empty;
        public int? GradingSystemId { get; set; }
    }
}