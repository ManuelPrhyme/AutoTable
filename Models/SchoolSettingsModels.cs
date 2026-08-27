namespace AutoTable.Models
{
    /// <summary>Global school configuration — name, head teacher, address.</summary>
    public class SchoolSettings
    {
        public string SchoolName { get; set; } = "AutoTable Academy";
        public string SchoolAddress { get; set; } = string.Empty;
        public string SchoolPhone { get; set; } = string.Empty;
        public string HeadTeacherName { get; set; } = string.Empty;
        public string Motto { get; set; } = string.Empty;
    }
}
