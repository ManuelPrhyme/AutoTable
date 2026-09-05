using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using AutoTable.Reports.ReportCards.Styling;

namespace AutoTable.Reports.ReportCards.Components
{
    /// <summary>
    /// Summary strip: Total | Average | Grade | Position | Status
    /// Five equal-width columns with light blue backgrounds.
    /// </summary>
    public sealed class SummaryStrip : IComponent
    {
        private readonly StudentReportCardData _student;
        private readonly ReportCardTheme _theme;

        public SummaryStrip(StudentReportCardData student, ReportCardTheme theme)
        {
            _student = student;
            _theme = theme;
        }

        public void Compose(IContainer container)
        {
            container.Row(row =>
            {
                row.Spacing(3);

                ComposeSummaryBox(row.RelativeItem(), "TOTAL", _student.TotalMarksDisplay);
                ComposeSummaryBox(row.RelativeItem(), "AVERAGE", $"{_student.OverallAverage:0.0}%");
                ComposeSummaryBox(row.RelativeItem(), "GRADE", _student.OverallGrade);
                ComposeSummaryBox(row.RelativeItem(), "POSITION", _student.PositionDisplay);
                ComposeSummaryBox(row.RelativeItem(), "STATUS", _student.Status);
            });
        }

        private void ComposeSummaryBox(IContainer container, string label, string value)
        {
            container.Element(c => ReportCardStyles.SummaryCell(c, _theme)).Column(col =>
            {
                col.Item()
                    .AlignCenter()
                    .Text(label)
                    .FontFamily(_theme.BodyFont)
                    .FontSize(7f)
                    .Bold()
                    .FontColor(_theme.MutedText);

                col.Item()
                    .PaddingTop(2)
                    .AlignCenter()
                    .Text(value)
                    .FontFamily(_theme.BodyFont)
                    .FontSize(10f)
                    .Bold()
                    .FontColor(_theme.Text);
            });
        }
    }
}