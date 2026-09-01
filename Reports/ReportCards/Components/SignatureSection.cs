using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoTable.Reports.ReportCards.Components
{
    /// <summary>
    /// Signature section: Principal | Class Teacher | Date — three equal-width zones.
    /// </summary>
    public sealed class SignatureSection : IComponent
    {
        private readonly StudentReportCardData _student;
        private readonly SchoolReportCardSettings _school;
        private readonly ReportCardTheme _theme;

        public SignatureSection(StudentReportCardData student, SchoolReportCardSettings school, ReportCardTheme theme)
        {
            _student = student;
            _school = school;
            _theme = theme;
        }

        public void Compose(IContainer container)
        {
            container.Row(row =>
            {
                row.Spacing(10);

                // Principal signature
                row.RelativeItem().Element(c => ComposeSlot(c,
                    "Principal",
                    _school.PrincipalName,
                    _school.PrincipalSignature));

                // Class teacher signature
                row.RelativeItem().Element(c => ComposeSlot(c,
                    "Class Teacher",
                    _student.ClassTeacherName,
                    _student.ClassTeacherSignature));

                // Date
                row.RelativeItem().Element(c => ComposeSlot(c,
                    "Date",
                    _student.ReportDate.ToString("dd MMMM yyyy"),
                    null));
            });
        }

        private void ComposeSlot(IContainer container, string role, string? name, byte[]? signature)
        {
            container.Column(col =>
            {
                // Signature image (if available)
                if (signature != null && signature.Length > 0)
                {
                    col.Item()
                        .Height(20)
                        .AlignCenter()
                        .Image(signature)
                        .FitArea();
                }

                // Signature line
                col.Item()
                    .PaddingTop(4)
                    .BorderBottom(0.5f)
                    .BorderColor(_theme.GridBlue);

                // Name
                if (!string.IsNullOrWhiteSpace(name))
                {
                    col.Item()
                        .PaddingTop(2)
                        .AlignCenter()
                        .Text(name)
                        .FontFamily(_theme.BodyFont)
                        .FontSize(8f)
                        .Bold()
                        .FontColor(_theme.Text);
                }

                // Role
                col.Item()
                    .AlignCenter()
                    .Text(role)
                    .FontFamily(_theme.BodyFont)
                    .FontSize(7.5f)
                    .FontColor(_theme.MutedText);
            });
        }
    }
}