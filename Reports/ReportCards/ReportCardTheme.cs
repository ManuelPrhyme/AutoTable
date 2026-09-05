namespace AutoTable.Reports.ReportCards
{
    /// <summary>
    /// Theme/design tokens for the report card PDF.
    /// Colors and fonts are centralized here so schools can customize branding
    /// without modifying layout components.
    /// </summary>
    public sealed class ReportCardTheme
    {
        // --- Color palette ---
        public string PrimaryNavy { get; init; } = "#00184D";
        public string SecondaryNavy { get; init; } = "#0A2860";
        public string AccentGold { get; init; } = "#E2A01B";
        public string LightBlue { get; init; } = "#ECF3FE";
        public string GridBlue { get; init; } = "#9FB4D2";
        public string Text { get; init; } = "#17213A";
        public string MutedText { get; init; } = "#53627A";
        public string White { get; init; } = "#FFFFFF";

        // --- Typography ---
        public string DisplayFont { get; init; } = "Georgia";
        public string BodyFont { get; init; } = "Lato";

        // --- Border / decoration ---
        public float OuterBorderThickness { get; init; } = 0.8f;
        public float TableBorderThickness { get; init; } = 0.6f;
        public float UnderlineBorderThickness { get; init; } = 0.5f;

        /// <summary>
        /// Creates a theme from school settings (or returns defaults when settings are null).
        /// </summary>
        public static ReportCardTheme From(
            string? primaryColor = null,
            string? secondaryColor = null,
            string? accentColor = null,
            string? tableBgColor = null,
            string? gridColor = null,
            string? bodyFont = null,
            string? displayFont = null)
        {
            return new ReportCardTheme
            {
                PrimaryNavy = primaryColor ?? "#00184D",
                SecondaryNavy = secondaryColor ?? "#0A2860",
                AccentGold = accentColor ?? "#E2A01B",
                LightBlue = tableBgColor ?? "#ECF3FE",
                GridBlue = gridColor ?? "#9FB4D2",
                Text = "#17213A",
                MutedText = "#53627A",
                White = "#FFFFFF",
                BodyFont = bodyFont ?? "Lato",
                DisplayFont = displayFont ?? "Georgia"
            };
        }
    }
}