using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using AutoTable.Reports.ReportCards.Styling;

namespace AutoTable.Reports.ReportCards.Components
{
    /// <summary>
    /// Principal's remark panel with light blue header and dynamic remark text.
    /// </summary>
    public sealed class PrincipalRemark : IComponent
    {
        private readonly StudentReportCardData _student;
        private readonly SchoolReportCardSettings _school;
        private readonly ReportCardTheme _theme;

        public PrincipalRemark(StudentReportCardData student, SchoolReportCardSettings school, ReportCardTheme theme)
        {
            _student = student;
            _school = school;
            _theme = theme;
        }

        public void Compose(IContainer container)
        {
            container.Column(col =>
            {
                // Header
                col.Item()
                    .Background(_theme.LightBlue)
                    .Padding(4)
                    .Text("PRINCIPAL'S REMARK")
                    .FontFamily(_theme.BodyFont)
                    .FontSize(8.5f)
                    .Bold()
                    .FontColor(_theme.PrimaryNavy);

                // Body
                col.Item()
                    .Border(0.6f)
                    .BorderColor(_theme.GridBlue)
                    .BorderTop(0)
                    .Padding(6)
                    .MinHeight(20)
                    .Text(string.IsNullOrWhiteSpace(_student.PrincipalRemark)
                        ? "—"
                        : _student.PrincipalRemark)
                    .FontFamily(_theme.BodyFont)
                    .FontSize(8.5f)
                    .FontColor(_theme.Text);

                // Principal name
                if (!string.IsNullOrWhiteSpace(_school.PrincipalName))
                {
                    col.Item()
                        .PaddingTop(2)
                        .AlignRight()
                        .Text($"Principal: {_school.PrincipalName}")
                        .FontFamily(_theme.BodyFont)
                        .FontSize(8f)
                        .SemiBold()
                        .FontColor(_theme.MutedText);
                }
            });
        }
    }
}