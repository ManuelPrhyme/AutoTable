using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoTable.Reports.ReportCards.Components
{
    /// <summary>
    /// Header section: logo | school identity (name, subtitle, motto, report title) | academic year ribbon.
    /// </summary>
    public sealed class ReportCardHeader : IComponent
    {
        private readonly SchoolReportCardSettings _school;
        private readonly ReportCardTheme _theme;

        public ReportCardHeader(SchoolReportCardSettings school, ReportCardTheme theme)
        {
            _school = school;
            _theme = theme;
        }

        public void Compose(IContainer container)
        {
            container.Column(col =>
            {
                // Three-zone row: Logo | Identity | Ribbon
                col.Item().Row(row =>
                {
                    // Logo zone (~26%)
                    row.RelativeItem(0.26f).Element(ComposeLogo);

                    // Identity zone (~59%)
                    row.RelativeItem(0.59f).Element(ComposeIdentity);

                    // Academic year ribbon (~15%)
                    row.RelativeItem(0.15f).Element(ComposeRibbon);
                });

                // Report Card ornament row
                col.Item().PaddingTop(2).Element(ComposeOrnament);
            });
        }

        private void ComposeLogo(IContainer container)
        {
            container
                .Padding(4)
                .AlignCenter()
                .AlignMiddle()
                .Width(42, QuestPDF.Infrastructure.Unit.Millimetre)
                .Height(40, QuestPDF.Infrastructure.Unit.Millimetre)
                .Border(0.7f)
                .BorderColor(_theme.GridBlue)
                .Background(_theme.White)
                .Image(_school.Logo)
                .FitArea();
        }

        private void ComposeIdentity(IContainer container)
        {
            container.PaddingHorizontal(6).Column(col =>
            {
                col.Spacing(1);

                // School name
                col.Item()
                    .AlignCenter()
                    .Text(_school.Name)
                    .FontFamily(_theme.DisplayFont)
                    .FontSize(25)
                    .Bold()
                    .FontColor(_theme.PrimaryNavy);

                // School subtitle
                if (!string.IsNullOrWhiteSpace(_school.Subtitle))
                {
                    col.Item()
                        .PaddingTop(1)
                        .AlignCenter()
                        .Text(_school.Subtitle)
                        .FontFamily(_theme.DisplayFont)
                        .FontSize(14)
                        .Bold()
                        .FontColor(_theme.PrimaryNavy);
                }

                // Motto
                if (!string.IsNullOrWhiteSpace(_school.Motto))
                {
                    col.Item()
                        .PaddingTop(2)
                        .AlignCenter()
                        .Text(_school.Motto)
                        .FontFamily(_theme.BodyFont)
                        .FontSize(9)
                        .Italic()
                        .FontColor(_theme.MutedText);
                }
            });
        }

        private void ComposeRibbon(IContainer container)
        {
            // Navy ribbon with academic year text
            container
                .Width(28, QuestPDF.Infrastructure.Unit.Millimetre)
                .Height(34, QuestPDF.Infrastructure.Unit.Millimetre)
                .Background(_theme.PrimaryNavy)
                .AlignCenter()
                .AlignMiddle()
                .Padding(4)
                .Column(col =>
                {
                    col.Item()
                        .AlignCenter()
                        .Text("ACADEMIC")
                        .FontFamily(_theme.BodyFont)
                        .FontSize(8)
                        .Bold()
                        .FontColor(_theme.White);

                    col.Item()
                        .AlignCenter()
                        .Text("YEAR")
                        .FontFamily(_theme.BodyFont)
                        .FontSize(8)
                        .Bold()
                        .FontColor(_theme.White);

                    col.Item()
                        .PaddingTop(3)
                        .AlignCenter()
                        .Text(_school.AcademicYear)
                        .FontFamily(_theme.BodyFont)
                        .FontSize(9)
                        .Bold()
                        .FontColor(_theme.AccentGold);
                });
        }

        private void ComposeOrnament(IContainer container)
        {
            // ── ◆ REPORT CARD ◆ ──
            container.Row(row =>
            {
                row.RelativeItem()
                    .PaddingTop(5)
                    .BorderBottom(1)
                    .BorderColor(_theme.AccentGold);

                row.ConstantItem(10)
                    .PaddingTop(3)
                    .AlignCenter()
                    .Text("◆")
                    .FontSize(10)
                    .FontColor(_theme.AccentGold);

                row.RelativeItem()
                    .PaddingTop(3)
                    .AlignCenter()
                    .Text("REPORT CARD")
                    .FontFamily(_theme.BodyFont)
                    .FontSize(16)
                    .Bold()
                    .FontColor(_theme.PrimaryNavy);

                row.ConstantItem(10)
                    .PaddingTop(3)
                    .AlignCenter()
                    .Text("◆")
                    .FontSize(10)
                    .FontColor(_theme.AccentGold);

                row.RelativeItem()
                    .PaddingTop(5)
                    .BorderBottom(1)
                    .BorderColor(_theme.AccentGold);
            });
        }
    }
}