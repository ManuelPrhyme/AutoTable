namespace AutoTable.Models
{
    public class TermFee
    {
        public int Id { get; set; }
        public int TermId { get; set; }
        public string TermName { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public double Amount { get; set; }
    }
}
