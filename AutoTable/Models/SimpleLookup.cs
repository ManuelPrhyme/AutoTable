namespace AutoTable.Models
{
    public class SimpleLookup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        // Optional: used by TermManagement to track active/ended state
        public bool? IsActive { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
