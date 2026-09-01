using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoTable.Reports.ReportCards.Styling
{
    /// <summary>
    /// Reusable cell and container style functions for consistent report card styling.
    /// </summary>
    public static class ReportCardStyles
    {
        /// <summary>
        /// Standard table header cell with light blue background.
        /// </summary>
        public static IContainer TableHeaderCell(IContainer container, ReportCardTheme theme)
        {
            return container
                .Background(theme.LightBlue)
                .BorderBottom(0.6f)
                .BorderColor(theme.GridBlue)
                .PaddingHorizontal(4)
                .PaddingVertical(3)
                .DefaultTextStyle(x => x
                    .FontFamily(theme.BodyFont)
                    .FontSize(7.5f)
                    .FontColor(theme.Text)
                    .SemiBold());
        }

        /// <summary>
        /// Standard table body cell with subtle grid borders.
        /// </summary>
        public static IContainer TableBodyCell(IContainer container, ReportCardTheme theme)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(theme.GridBlue)
                .PaddingHorizontal(4)
                .PaddingVertical(2.5f)
                .DefaultTextStyle(x => x
                    .FontFamily(theme.BodyFont)
                    .FontSize(8.5f)
                    .FontColor(theme.Text));
        }

        /// <summary>
        /// Section header bar (navy background, white text).
        /// </summary>
        public static void SectionHeaderBar(IContainer container, ReportCardTheme theme, string text)
        {
            container
                .Background(theme.PrimaryNavy)
                .Padding(5)
                .AlignCenter()
                .AlignMiddle()
                .Text(text)
                .FontFamily(theme.BodyFont)
                .FontSize(9.5f)
                .Bold()
                .FontColor(theme.White);
        }

        /// <summary>
        /// Summary strip cell with light blue background.
        /// </summary>
        public static IContainer SummaryCell(IContainer container, ReportCardTheme theme)
        {
            return container
                .Border(0.5f)
                .BorderColor(theme.GridBlue)
                .Background(theme.LightBlue)
                .PaddingHorizontal(5)
                .PaddingVertical(4)
                .AlignCenter();
        }

        /// <summary>
        /// Comment/remark panel header (navy background, white text).
        /// </summary>
        public static void PanelHeader(IContainer container, ReportCardTheme theme, string text)
        {
            container
                .Background(theme.PrimaryNavy)
                .Padding(4)
                .AlignCenter()
                .AlignMiddle()
                .Text(text)
                .FontFamily(theme.BodyFont)
                .FontSize(9f)
                .Bold()
                .FontColor(theme.White);
        }

        /// <summary>
        /// Student info label style (bold uppercase, muted).
        /// </summary>
        public static IContainer InfoLabel(IContainer container, ReportCardTheme theme)
        {
            return container
                .DefaultTextStyle(x => x
                    .FontFamily(theme.BodyFont)
                    .FontSize(8f)
                    .FontColor(theme.MutedText)
                    .SemiBold());
        }

        /// <summary>
        /// Student info value with underline border.
        /// </summary>
        public static IContainer InfoValue(IContainer container, ReportCardTheme theme)
        {
            return container
                .BorderBottom(theme.UnderlineBorderThickness)
                .BorderColor(theme.GridBlue)
                .PaddingBottom(1)
                .DefaultTextStyle(x => x
                    .FontFamily(theme.BodyFont)
                    .FontSize(8.5f)
                    .FontColor(theme.Text));
        }
    }
}