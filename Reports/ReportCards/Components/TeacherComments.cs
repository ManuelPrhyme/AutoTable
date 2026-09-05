using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using AutoTable.Reports.ReportCards.Styling;

namespace AutoTable.Reports.ReportCards.Components
{
    /// <summary>
    /// Teacher's comments panel with navy header and dynamic comment text.
    /// </summary>
    public sealed class TeacherComments : IComponent
    {
        private readonly StudentReportCardData _student;
        private readonly ReportCardTheme _theme;

        public TeacherComments(StudentReportCardData student, ReportCardTheme theme)
        {
            _student = student;
            _theme = theme;
        }

        public void Compose(IContainer container)
        {
            container.Column(col =>
            {
                // Panel header
                col.Item().Element(c => ReportCardStyles.PanelHeader(c, _theme, "TEACHER'S COMMENTS"));

                // Body
                col.Item()
                    .Border(0.6f)
                    .BorderColor(_theme.GridBlue)
                    .BorderTop(0)
                    .Padding(6)
                    .MinHeight(40)
                    .Text(string.IsNullOrWhiteSpace(_student.TeacherComments)
                        ? "—"
                        : _student.TeacherComments)
                    .FontFamily(_theme.BodyFont)
                    .FontSize(8.5f)
                    .FontColor(_theme.Text);

                // Class teacher name
                if (!string.IsNullOrWhiteSpace(_student.ClassTeacherName))
                {
                    col.Item()
                        .PaddingTop(2)
                        .AlignRight()
                        .Text($"Class Teacher: {_student.ClassTeacherName}")
                        .FontFamily(_theme.BodyFont)
                        .FontSize(8f)
                        .SemiBold()
                        .FontColor(_theme.MutedText);
                }
            });
        }
    }
}