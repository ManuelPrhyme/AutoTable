// Minimal stubs for models that depend on WinUI types (e.g. Visibility).
// The test project only needs the data-layer types, not WinUI display helpers.

namespace AutoTable.Models
{
    public class GradingSystemInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public double PassMark { get; set; } = 50;
        public System.Collections.Generic.List<GradeBandInfo> Bands { get; } = new();
        public string BandsSummary => string.Empty;
    }

    public class GradeBandInfo
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public double MinScore { get; set; }
        public double MaxScore { get; set; }
        public bool IsPromotionalPass { get; set; }
        public bool IsRepeater { get; set; }
        public bool IsPromotionalFail { get; set; }
    }
}
