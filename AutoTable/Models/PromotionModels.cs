namespace AutoTable.Models
{
    /// <summary>
    /// End-of-year promotion decision. 0 = Pending, 1 = Promoted,
    /// 2 = Repeat, 3 = Shifted (manual class change / skip-ahead).
    /// </summary>
    public enum PromotionStatus
    {
        Pending = 0,
        Promoted = 1,
        Repeat = 2,
        Shifted = 3
    }

    /// <summary>
    /// One student's promotion overview row: their terminal average, the class
    /// pass mark (set by the class author on the grading system), the suggested
    /// outcome, and the recorded decision that can be applied (promote/repeat/shift).
    /// </summary>
    public class PromotionRow
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string LIN { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Stream { get; set; } = string.Empty;

        public double Average { get; set; }
        public double PassMark { get; set; } = 50;
        public string Grade { get; set; } = "-";

        // Suggested outcome (above pass mark -> promote, below -> repeat).
        public PromotionStatus Suggested { get; set; } = PromotionStatus.Pending;

        // Current recorded decision (Pending until the author processes the class).
        public PromotionStatus Status { get; set; } = PromotionStatus.Pending;

        // Where the student will go when promoted (next class by numeric suffix),
        // or the manual shift target.
        public int? TargetClassId { get; set; }
        public string TargetClassName { get; set; } = string.Empty;

        // Display helpers for x:Bind
        public string StatusLabel => Status switch
        {
            PromotionStatus.Promoted => "Promoted",
            PromotionStatus.Repeat => "Repeat",
            PromotionStatus.Shifted => "Shifted",
            _ => "Pending"
        };

        public string SuggestedLabel => Suggested == PromotionStatus.Promoted ? "Promote" : "Repeat";

        /// <summary>True when the recorded decision matches what the marks suggest is wrong.</summary>
        public bool IsPassing => Average >= PassMark;

        public string AverageDisplay => Average > 0 ? Average.ToString("0.##") : "—";
    }
}
