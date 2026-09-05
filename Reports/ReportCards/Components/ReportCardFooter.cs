using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoTable.Reports.ReportCards.Components
{
    /// <summary>
    /// Footer section: navy bar with school contact details (address, phone, email, website).
    /// </summary>
    public sealed class ReportCardFooter : IComponent
    {
        private readonly SchoolReportCardSettings _school;
        private readonly ReportCardTheme _theme;

        public ReportCardFooter(SchoolReportCardSettings school, ReportCardTheme theme)
        {
            _school = school;
            _theme = theme;
        }

        public void Compose(IContainer container)
        {
            container
                .Background(_theme.PrimaryNavy)
                .PaddingHorizontal(10)
                .PaddingVertical(5)
                .Row(row =>
                {
                    row.Spacing(15);

                    // Address
                    if (!string.IsNullOrWhiteSpace(_school.PostalAddress))
                    {
                        row.RelativeItem(0.30f)
                            .AlignCenter()
                            .Text(_school.PostalAddress)
                            .FontFamily(_theme.BodyFont)
                            .FontSize(7.5f)
                            .FontColor(_theme.White);
                    }

                    // Phone
                    if (!string.IsNullOrWhiteSpace(_school.Phone))
                    {
                        row.RelativeItem(0.21f)
                            .AlignCenter()
                            .Text(_school.Phone)
                            .FontFamily(_theme.BodyFont)
                            .FontSize(7.5f)
                            .FontColor(_theme.White);
                    }

                    // Email
                    if (!string.IsNullOrWhiteSpace(_school.Email))
                    {
                        row.RelativeItem(0.25f)
                            .AlignCenter()
                            .Text(_school.Email)
                            .FontFamily(_theme.BodyFont)
                            .FontSize(7.5f)
                            .FontColor(_theme.White);
                    }

                    // Website
                    if (!string.IsNullOrWhiteSpace(_school.Website))
                    {
                        row.RelativeItem(0.24f)
                            .AlignCenter()
                            .Text(_school.Website)
                            .FontFamily(_theme.BodyFont)
                            .FontSize(7.5f)
                            .FontColor(_theme.White);
                    }
                });
        }
    }
}