using System.Collections.Generic;
using System.Linq;

namespace AutoTable.Models
{
    /// <summary>
    /// A named grading system (e.g. "Uganda PLE", "IGCSE") that a class can use.
    /// Contains ordered grade bands defining what each mark range is called and
    /// whether the band promotes, repeats, or fails the student for promotion.
    /// </summary>
    public class GradingSystemInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        // The mark (%) a student must average to be promoted under this system.
        // The class author sets this when creating the grading scale.
        public double PassMark { get; set; } = 50;
        public List<GradeBandInfo> Bands { get; } = new List<GradeBandInfo>();

        // Display helpers for x:Bind
        public Microsoft.UI.Xaml.Visibility DefaultVisibility =>
            IsDefault ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        public string BandsSummary => Bands.Count == 0
            ? "(no grade bands defined yet)"
            : string.Join(", ", Bands.Select(b => $"{b.Label} - {b.MinScore:0}-{b.MaxScore:0}{(b.IsPromotionalPass ? " ✓" : b.IsRepeater ? " ↻" : b.IsPromotionalFail ? " ✗" : "")}"));

        public override string ToString() =>
            IsDefault ? $"{Name} (default)" : Name;
    }

    /// <summary>
    /// One grade band inside a grading system, e.g. Label "A" covering 90–100.
    /// The flags determine what a mark in this range means for promotion:
    /// IsPromotionalPass → promoted, IsRepeater → repeat the class,
    /// IsPromotionalFail → fails promotion outright.
    /// </summary>
    public class GradeBandInfo
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public double MinScore { get; set; }
        public double MaxScore { get; set; }
        public bool IsPromotionalPass { get; set; }
        public bool IsRepeater { get; set; }
        public bool IsPromotionalFail { get; set; }

        public override string ToString() => $"{Label} ({MinScore:0}–{MaxScore:0})";
    }
}