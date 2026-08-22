using System;

namespace AutoTable.Models
{
    public class AssessmentItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        // Optional: target a specific stream (null means whole class unless IsClassWide=false and StreamId set)
        public int? StreamId { get; set; }
        public string? StreamName { get; set; }
        public bool IsClassWide { get; set; } = true;
        public int WeightPercent { get; set; }
        public DateTime DueDate { get; set; }
        public int MarksEnteredPercent { get; set; }
        public bool IsVerified { get; set; }
        public bool IsPublished { get; set; }
        public string StatusLabel => IsPublished ? "Published" : IsVerified ? "Verified" : MarksEnteredPercent >= 100 ? "Complete" : "In Progress";
    }
}
