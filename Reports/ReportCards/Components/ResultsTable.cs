using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using AutoTable.Reports.ReportCards.Styling;

namespace AutoTable.Reports.ReportCards.Components
{
    /// <summary>
    /// Examination results section: navy title bar + dynamic subject results table.
    /// </summary>
    public sealed class ResultsTable : IComponent
    {
        private readonly IReadOnlyList<SubjectResult> _subjects;
        private readonly ReportCardTheme _theme;

        public ResultsTable(IReadOnlyList<SubjectResult> subjects, ReportCardTheme theme)
        {
            _subjects = subjects;
            _theme = theme;
        }

        public void Compose(IContainer container)
        {
            container.Column(col =>
            {
                // Section title bar
                col.Item().Element(c => ReportCardStyles.SectionHeaderBar(c, _theme, "PROMOTIONAL EXAMINATION RESULTS"));

                // Table
                col.Item().PaddingTop(2).Element(ComposeTable);
            });
        }

        private void ComposeTable(IContainer container)
        {
            container.Border(0.6f).BorderColor(_theme.GridBlue).Table(table =>
            {
                // Column definitions: Subject(1.75), Max(1), CAT1(1), CAT2(1), Exam(1), Total(1), Avg(1), Grade(0.9)
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.75f);  // Subject
                    columns.RelativeColumn(1f);     // Max Marks
                    columns.RelativeColumn(1f);     // CAT 1
                    columns.RelativeColumn(1f);     // CAT 2
                    columns.RelativeColumn(1f);     // Exam
                    columns.RelativeColumn(1f);     // Total
                    columns.RelativeColumn(1f);     // Average
                    columns.RelativeColumn(0.9f);   // Grade
                });

                // Header row
                table.Header(header =>
                {
                    header.Cell().Element(c => HeaderCell(c, "SUBJECT"));
                    header.Cell().Element(c => HeaderCell(c, "MAX\nMARKS"));
                    header.Cell().Element(c => HeaderCell(c, "CAT 1\n(%)"));
                    header.Cell().Element(c => HeaderCell(c, "CAT 2\n(%)"));
                    header.Cell().Element(c => HeaderCell(c, "EXAM\n(%)"));
                    header.Cell().Element(c => HeaderCell(c, "TOTAL\n(%)"));
                    header.Cell().Element(c => HeaderCell(c, "AVERAGE\n(%)"));
                    header.Cell().Element(c => HeaderCell(c, "GRADE"));
                });

                // Body rows
                foreach (var subject in _subjects)
                {
                    table.Cell().Element(c => BodyCell(c, subject.SubjectName, true));
                    table.Cell().Element(c => BodyCell(c, subject.MaximumMarks.ToString(), false));
                    table.Cell().Element(c => BodyCell(c, FormatMark(subject.Cat1), false));
                    table.Cell().Element(c => BodyCell(c, FormatMark(subject.Cat2), false));
                    table.Cell().Element(c => BodyCell(c, FormatMark(subject.Exam), false));
                    table.Cell().Element(c => BodyCell(c, FormatMark(subject.Total), false));
                    table.Cell().Element(c => BodyCell(c, FormatMark(subject.Average), false));
                    table.Cell().Element(c => GradeCell(c, subject.Grade));
                }
            });
        }

        private void HeaderCell(IContainer container, string text)
        {
            container
                .Background(_theme.LightBlue)
                .BorderBottom(0.6f)
                .BorderColor(_theme.GridBlue)
                .PaddingHorizontal(4)
                .PaddingVertical(3)
                .AlignCenter()
                .Text(text)
                .FontFamily(_theme.BodyFont)
                .FontSize(7.5f)
                .FontColor(_theme.Text)
                .SemiBold();
        }

        private void BodyCell(IContainer container, string text, bool alignLeft)
        {
            var styled = container
                .BorderBottom(0.5f)
                .BorderColor(_theme.GridBlue)
                .PaddingHorizontal(4)
                .PaddingVertical(2.5f)
                .DefaultTextStyle(x => x
                    .FontFamily(_theme.BodyFont)
                    .FontSize(8.5f)
                    .FontColor(_theme.Text));

            if (alignLeft)
                styled.Text(text);
            else
                styled.AlignCenter().Text(text);
        }

        private void GradeCell(IContainer container, string grade)
        {
            container
                .BorderBottom(0.5f)
                .BorderColor(_theme.GridBlue)
                .PaddingHorizontal(4)
                .PaddingVertical(2.5f)
                .AlignCenter()
                .Text(grade)
                .FontFamily(_theme.BodyFont)
                .FontSize(8.5f)
                .FontColor(_theme.Text)
                .Bold();
        }

        private static string FormatMark(decimal value)
        {
            return value > 0 ? value.ToString("0.#") : "—";
        }
    }
}