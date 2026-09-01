using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using AutoTable.Reports.ReportCards.Styling;

namespace AutoTable.Reports.ReportCards.Components
{
    /// <summary>
    /// Student information section: two-column field grid on the left, student photo on the right.
    /// </summary>
    public sealed class StudentInformation : IComponent
    {
        private readonly StudentReportCardData _student;
        private readonly ReportCardTheme _theme;

        public StudentInformation(StudentReportCardData student, ReportCardTheme theme)
        {
            _student = student;
            _theme = theme;
        }

        public void Compose(IContainer container)
        {
            container.Row(row =>
            {
                // Left: field grid (~72%)
                row.RelativeItem(0.72f).Element(ComposeFieldGrid);

                // Right: student photo (~18%)
                row.RelativeItem(0.18f).Element(ComposePhoto);

                // Spacer (~10%)
                row.RelativeItem(0.10f);
            });
        }

        private void ComposeFieldGrid(IContainer container)
        {
            container.Border(0.6f).BorderColor(_theme.GridBlue).Padding(6).Column(col =>
            {
                col.Spacing(4);

                // Row 1: Name + DOB
                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => ComposeField(c, "STUDENT NAME", _student.Name));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => ComposeField(c, "DATE OF BIRTH", FormatDate(_student.DateOfBirth)));
                });

                // Row 2: Admission + Gender
                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => ComposeField(c, "ADMISSION NO", _student.AdmissionNumber));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => ComposeField(c, "GENDER", _student.Gender));
                });

                // Row 3: Class + House
                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => ComposeField(c, "CLASS", _student.ClassName));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => ComposeField(c, "HOUSE", _student.House));
                });

                // Row 4: Term + Report Date
                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => ComposeField(c, "TERM", _student.Term));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => ComposeField(c, "REPORT DATE", FormatDate(_student.ReportDate)));
                });

                // Row 5: Academic Year (full width)
                col.Item().Element(c => ComposeField(c, "ACADEMIC YEAR", _student.AcademicYear));
            });
        }

        private void ComposeField(IContainer container, string label, string? value)
        {
            container.Column(col =>
            {
                col.Item().Text(label).FontFamily(_theme.BodyFont).FontSize(8f).SemiBold().FontColor(_theme.MutedText);
                col.Item()
                    .BorderBottom(_theme.UnderlineBorderThickness)
                    .BorderColor(_theme.GridBlue)
                    .PaddingBottom(1)
                    .Text(string.IsNullOrWhiteSpace(value) ? "—" : value)
                    .FontFamily(_theme.BodyFont)
                    .FontSize(8.5f)
                    .FontColor(_theme.Text);
            });
        }

        private void ComposePhoto(IContainer container)
        {
            var photo = _student.Photo;
            if (photo == null || photo.Length == 0)
            {
                // Placeholder
                container
                    .Border(0.7f)
                    .BorderColor(_theme.GridBlue)
                    .Background(_theme.LightBlue)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("PHOTO")
                    .FontFamily(_theme.BodyFont)
                    .FontSize(8)
                    .FontColor(_theme.MutedText);
                return;
            }

            container
                .Border(0.7f)
                .BorderColor(_theme.GridBlue)
                .Padding(2)
                .Image(photo)
                .FitArea();
        }

        private static string FormatDate(DateOnly? date)
        {
            return date.HasValue ? date.Value.ToString("dd MMMM yyyy") : "—";
        }

        private static string FormatDate(DateOnly date)
        {
            return date.ToString("dd MMMM yyyy");
        }
    }
}