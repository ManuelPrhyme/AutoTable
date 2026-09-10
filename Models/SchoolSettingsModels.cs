namespace AutoTable.Models
{
    /// <summary>Global school configuration — name, head teacher, address, logo.</summary>
    public class SchoolSettings
    {
        public string SchoolName { get; set; } = "AutoTable Academy";
        public string SchoolAddress { get; set; } = string.Empty;
        public string SchoolPhone { get; set; } = string.Empty;
        public string HeadTeacherName { get; set; } = string.Empty;
        public string Motto { get; set; } = string.Empty;
        /// <summary>Blockchain instance address derived from this device's key pair (read-only, set at first-run).</summary>
        public string InstanceAddress { get; set; } = string.Empty;
        /// <summary>Raw image bytes for the school logo.</summary>
        public byte[]? LogoBytes { get; set; }
    }
}
